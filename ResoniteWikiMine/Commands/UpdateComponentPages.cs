using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;
using static ResoniteWikiMine.Utility.ComponentBatchUpdater;

namespace ResoniteWikiMine.Commands;

public sealed class UpdateComponentPages : ICommand
{
    private static readonly (string frooxCategory, string wikiCategory)[] CategoryDefinitions =
    [
        ("", "Components"),
        ("Assets/Materials", "Materials")
    ];

    public async Task<int> Run(WorkContext context, string[] args)
    {
        return UpdateComponentPages(
            context,
            _ => true,
            page => GenerateNewPageContent(page.Type, page.Content));
    }

    public static BatchUpdatePageResult? GenerateNewPageContent(Type type, string content)
    {
        var prevContent = content;
        var changes = PageChanges.None;

        content = UpdateComponentFields(type, type.Name, content);
        CheckChange(PageChanges.Fields);
        //content = UpdateSyncDelegateFields(type, type.Name, content);
        //CheckChange(PageChanges.SyncDelegates);
        content = UpdateComponentPageCategories(type, type.Name, content);
        CheckChange(PageChanges.Categories);



        if (changes == PageChanges.None || content.Equals(prevContent))
        {
            Console.WriteLine("no changes to \"" + type.Name + "\"");
            return null;
        }
        else
        {
            var parser = new WikitextParser();
            var parsed = parser.Parse(content);
            var categories = CategoryHelper.GetCategories(parsed);
            CategoryHelper.EnsureCategoryState(categories, "Category:ComponentStubs", true);
            content = parsed.ToString();
            CheckChange(PageChanges.Categories);
        }


        return new BatchUpdatePageResult
        {
            NewContent = content, ChangeDescription = $"update {changes.ToString()}"
        };

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

        var fieldDescriptions = ParseTableFields(fieldsTemplate);

        return SpliceString(
            content,
            fieldsTemplate.Range,
            FieldFormatter.MakeComponentFieldsTemplate(type, fieldDescriptions));
    }

    private static string UpdateSyncDelegateFields(Type type, string name, string content)
    {
        var fieldsTemplate = PageContentParser.GetTemplateInPage(content, "Table ComponentTriggers");
        var newcontent = "";
        if (fieldsTemplate == null)
        {
            var componentfields = PageContentParser.GetTemplateInPage(content, "Table ComponentFields");
            if (componentfields != null)
            {
                newcontent = content.Insert(componentfields.Range.End.Value, "\n\n== Sync Delegates ==\n{{Table ComponentTriggers\n}}");

                fieldsTemplate = PageContentParser.GetTemplateInPage(newcontent, "Table ComponentTriggers");

                if (fieldsTemplate == null) return content;
                Console.WriteLine($"found ComponentFields in page for {name}, generating in absence of ComponentTriggers!");
            }
            else
            {
                Console.WriteLine($"Unable to find Table ComponentTriggers in page for {name}");
                return content;
            }
        }

        var fieldDescriptions = ParseTableFields(fieldsTemplate);
        string newcontent3 = SyncDelegateFormatter.MakeSyncDelegatesTemplate(type, fieldDescriptions);

        if (!"{{Table ComponentTriggers\n}}".Equals(newcontent3))
        {
            if(newcontent != "")
            {
                content = newcontent;
            }
            Console.WriteLine($"updating  ComponentTriggers for {name}, {fieldsTemplate == null}");
            return SpliceString(
            content,
            fieldsTemplate.Range,
            newcontent3);
        }
        return content;
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

    private static void UpdateComponentCategoryCategories(Type type, CategoryHelper.CategoryData categories,
        string niceName)
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
            if (possiblyNested is { IsGenericTypeParameter: false, IsNested: true } &&
                possiblyNested.DeclaringType == type)
            {
                if (possiblyNested.IsEnum)
                    nestedEnums = true;
                else
                    nestedTypes = true;
            }
        }

        return (nestedTypes, nestedEnums);
    }

    public static Dictionary<string, string> ParseTableFields(PageContentParser.TemplateMatch template)
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

    public static string SpliceString(string text, Range range, string replacement)
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
        SyncDelegates = 1 << 2,
    }
}
