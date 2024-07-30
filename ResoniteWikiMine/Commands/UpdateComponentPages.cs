using Dapper;
using FrooxEngine;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;

namespace ResoniteWikiMine.Commands;

public sealed class UpdateComponentPages : ICommand
{
    private static readonly (string frooxCategory, string wikiCategory)[] CategoryDefinitions = [
        ("", "Components"),
        ("Assets/Materials", "Materials")
    ];

    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        var prevRet = WikiComponentReport.RunCoreTransacted(context, args);
        if (!prevRet)
            return 1;

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Start UpdateComponentPages");
        Console.ResetColor();

        db.Execute("DROP VIEW IF EXISTS wiki_component_update_report_view");
        db.Execute("DROP TABLE IF EXISTS wiki_component_update_report");
        db.Execute("""
            CREATE TABLE wiki_component_update_report (
                name TEXT PRIMARY KEY NOT NULL REFERENCES wiki_component_report(name),
                new_text TEXT NOT NULL,
                changes INT NOT NULL,
                changes_text TEXT NOT NULL,
                diff TEXT NOT NULL
            );
            """);

        db.Execute("""
            CREATE VIEW wiki_component_update_report_view AS
            SELECT wcur.name, wcr.category, pc.content old_text, wcur.new_text, wcur.changes_text, wcur.diff
            FROM wiki_component_update_report wcur
            INNER JOIN wiki_component_report wcr ON wcur.name = wcr.name
            INNER JOIN page_content pc ON pc.id = wcr.page
            WHERE wcur.diff != ''
            ORDER BY 1
            """);

        var components = db.Query<(string name, string fullName, string content)>("""
            SELECT
                wcr.name, wcr.full_name, pc.content
            FROM wiki_component_report wcr
            INNER JOIN main.page_content pc ON wcr.page = pc.id AND pc.slot = 'main'
            ORDER BY 1
            """);

        foreach (var (name, fullname, content) in components)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Component: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(name);
            Console.ResetColor();

            try
            {
                var newContent = GenerateNewPageContent(name, fullname, content);
                if (newContent == null)
                    continue;

                var (newContentText, changes) = newContent.Value;

                var diff = DiffFormatter.GenerateDiff(content, newContentText);

                db.Execute(
                    "INSERT INTO wiki_component_update_report (name, new_text, diff, changes, changes_text) " +
                    "VALUES (@Name, @NewContent, @Diff, @Changes, @ChangesText)",
                    new
                    {
                        Name = name,
                        NewContent = newContentText,
                        Diff = diff,
                        Changes = changes,
                        ChangesText = changes.ToString()
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while processing {name}: {e}");
            }
        }

        await transaction.CommitAsync();

        return 0;
    }

    public static (string newContent, PageChanges changes)? GenerateNewPageContent(string name, string fullname, string content)
    {
        var type = FrooxLoader.GetType(fullname);
        if (type == null)
        {
            Console.WriteLine($"Unable to find .NET type for {name} ???");
            return null;
        }

        var prevContent = content;
        var changes = PageChanges.None;

        content = UpdateComponentFields(type, name, content);
        CheckChange(PageChanges.Fields);
        content = UpdateComponentPageCategories(type, name, content);
        CheckChange(PageChanges.Categories);

        return (content, changes);

        void CheckChange(PageChanges change)
        {
            if (prevContent != content)
            {
                prevContent = content;
                changes |= change;
            }
        }
    }

    private static string UpdateComponentFields(Type type, string name, string content)
    {
        var fieldsTemplate = PageContentParser.GetTemplateInPage(content, "Table ComponentFields");
        if (fieldsTemplate == null)
        {
            Console.WriteLine($"Unable to find Table ComponentFields in page for {name}");
            return content;
        }

        var fieldDescriptions = ParseComponentFields(fieldsTemplate);

        return SpliceString(
            content,
            fieldsTemplate.Range,
            FieldFormatter.MakeComponentFieldsTemplate(type, fieldDescriptions));
    }

    private static string UpdateComponentPageCategories(Type type, string name, string content)
    {
        var parser = new WikitextParser();
        var parsed = parser.Parse(content);
        var categories = CategoryHelper.GetCategories(parsed);

        if (categories.Categories.Count == 0)
        {
            Console.WriteLine($"Unable to find any categories in page for {name}!");
            return content;
        }

        var niceName = CreateComponentPages.GetNiceName(type);

        var (nestedTypes, nestedEnums) = CheckHasNestedTypes(type);
        var isGeneric = type.IsGenericType;

        CategoryHelper.EnsureCategoryState(
            categories,
            "Category:Components With Nested Types{{#translation:}}",
            nestedTypes,
            niceName);

        CategoryHelper.EnsureCategoryState(
            categories,
            "Category:Components With Nested Enums{{#translation:}}",
            nestedEnums,
            niceName);

        CategoryHelper.EnsureCategoryState(
            categories,
            "Category:Generic Components{{#translation:}}",
            isGeneric,
            niceName);

        CategoryHelper.EnsureCategoryState(
            categories,
            "Category:Components{{#translation:}}",
            true,
            niceName);

        CategoryHelper.EnsureCategoryState(
            categories,
            "Category:Materials{{#translation:}}",
            IsInCategory(type, "Assets/Materials"),
            niceName);

        // Remove categories without translation markers that shouldn't be used.
        CategoryHelper.EnsureCategoryState(categories, "Category:Components", false);
        CategoryHelper.EnsureCategoryState(categories, "Category:Materials", false);
        CategoryHelper.EnsureCategoryState(categories, "Category:Components With Nested Types", false);
        CategoryHelper.EnsureCategoryState(categories, "Category:Components With Nested Enums", false);

        // Synchronize category categories (Components:Assets and so on)
        UpdateComponentCategoryCategories(type, categories, niceName);

        return parsed.ToString();
    }

    private static void UpdateComponentCategoryCategories(Type type, CategoryHelper.CategoryData categories, string niceName)
    {
        var expectedCategories = new HashSet<string>();
        var pageCategories = new List<WikiLink>();

        foreach (var (frooxCategory, wikiCategory) in CategoryDefinitions)
        {
            foreach (var category in CreateComponentPages.GetComponentCategory(type))
            {
                if (CategoryRelativeTo(category, frooxCategory) is { } relative && relative != "")
                {
                    var colons = relative.Replace('/', ':');
                    expectedCategories.Add($"Category:{wikiCategory}:{colons}{{{{#translation:}}}}");
                }
            }

            pageCategories.AddRange(categories.Categories
                .Where(x => x.Target.ToString().StartsWith($"Category:{wikiCategory}:")));
        }

        foreach (var pageCategory in pageCategories)
        {
            if (!expectedCategories.Contains(pageCategory.Target.ToString()))
                CategoryHelper.RemoveCategory(categories, pageCategory);
        }

        foreach (var expectedCategory in expectedCategories)
        {
            CategoryHelper.EnsureCategoryState(categories, expectedCategory, true, niceName);
        }
    }

    private static bool IsInCategory(Type type, string category)
    {
        return CreateComponentPages.GetComponentCategory(type).Any(x => CategoryRelativeTo(x, category) != null);
    }

    private static string? CategoryRelativeTo(string category, string categoryBase)
    {
        var splitA = category.Split('/');
        var splitB = categoryBase.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (!splitA.Take(splitB.Length).SequenceEqual(splitB))
            return null;

        return string.Join('/', splitA.Skip(splitB.Length));
    }

    private static (bool types, bool enums) CheckHasNestedTypes(Type type)
    {
        var allTypes = FieldFormatter.EnumerateSyncFields(type)
            .SelectMany(entry => TypeHelper.GenericTypesRecursive(entry.Type));

        var nestedTypes = false;
        var nestedEnums = false;

        foreach (var possiblyNested in allTypes)
        {
            if (possiblyNested is { IsGenericTypeParameter: false, IsNested: true } && possiblyNested.DeclaringType == type)
            {
                if (possiblyNested.IsEnum)
                    nestedEnums = true;
                else
                    nestedTypes = true;
            }
        }

        return (nestedTypes, nestedEnums);
    }

    private static Dictionary<string, string> ParseComponentFields(PageContentParser.TemplateMatch template)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var args = template.PositionalArguments;
        for (var i = 0; i < args.Length; i += 3)
        {
            var name = template.PositionalArguments[i];
            var description = template.PositionalArguments[i + 2].TrimEnd();
            if (!fields.TryAdd(name, description))
                Console.WriteLine($"Duplicate property: {name}");
        }

        return fields;
    }

    private static string SpliceString(string text, Range range, string replacement)
    {
        return string.Concat(
            text.AsSpan(0, range.Start.GetOffset(text.Length)),
            replacement,
            text.AsSpan(range.End.GetOffset(text.Length)));
    }

    [Flags]
    public enum PageChanges
    {
        None = 0,
        Fields = 1 << 0,
        Categories = 1 << 1,
    }
}
