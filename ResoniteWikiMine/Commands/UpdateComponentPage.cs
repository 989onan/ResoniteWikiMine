using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;
using static ResoniteWikiMine.Utility.ComponentBatchUpdater;

namespace ResoniteWikiMine.Commands;

public sealed class UpdateComponentPage : ICommand
{
    private static readonly (string frooxCategory, string wikiCategory)[] CategoryDefinitions =
    [
        ("", "Components"),
        ("Assets/Materials", "Materials")
    ];

    public async Task<int> Run(WorkContext context, string[] args)
    {
        return UpdateComponentPage(
            context,
            _ => true,
            page => GenerateNewPageContent(page.Type, page.Content), args[0]);
    }

    public static BatchUpdatePageResult? GenerateNewPageContent(Type type, string content)
    {
        var prevContent = content;
        var changes = PageChanges.None;

        content = UpdateComponentFields(type, type.Name, content);
        content = UpdateSyncDelegateFields(type, type.Name, content);
        CheckChange(PageChanges.Fields);
        content = UpdateComponentPageCategories(type, type.Name, content);
        CheckChange(PageChanges.Categories);

        if (changes == PageChanges.None)
            return null;

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
        if (fieldsTemplate == null)
        {
            Console.WriteLine($"Unable to find Table ComponentTriggers in page for {name}");
            return content;
        }

        var fieldDescriptions = ParseTableFields(fieldsTemplate);

        return SpliceString(
            content,
            fieldsTemplate.Range,
            SyncDelegateFormatter.MakeSyncDelegatesTemplate(type, fieldDescriptions));
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

    public static string? CategoryRelativeTo(string category, string categoryBase)
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
    }
}
