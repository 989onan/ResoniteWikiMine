using MwParserFromScratch.Nodes;

namespace ResoniteWikiMine.MediaWiki;

/// <summary>
/// Helper class for working with categories in wikitext.
/// </summary>
public static class CategoryHelper
{
    public static CategoryData GetCategories(Wikitext mainNode)
    {
        var links = mainNode
            .EnumDescendants()
            .OfType<WikiLink>()
            .Where(link => link.Target.ToPlainText().StartsWith("Category:"))
            .ToList();

        return new CategoryData(mainNode, links);
    }

    public static void RemoveCategory(CategoryData data, WikiLink category)
    {
        if (!data.Categories.Remove(category))
            return;

        if (IsItsOwnLine(category))
            category.NextNode.Remove();

        category.Remove();
    }

    public static void AddCategory(CategoryData data, WikiLink category)
    {
        var newLine = new PlainText("\n");
        data.Categories[^1].InsertAfter(newLine);
        newLine.InsertAfter(category);

        data.Categories.Add(category);
    }

    public static void AddCategory(CategoryData data, string target, string? text = null)
    {
        AddCategory(data, NewCategoryLink(target, text));
    }

    public static void EnsureCategoryState(CategoryData data, string target, bool present, string? text = null)
    {
        var match = data.Categories.SingleOrDefault(link => link.Target.ToString() == target);
        if (match == null && present)
        {
            AddCategory(data, NewCategoryLink(target, text));
        }
        else if (match != null)
        {
            if (!present)
            {
                RemoveCategory(data, match);
            }
            else if (text != null)
            {
                // Update text for existing category link.
                match.Text = PlainTextToRun(text);
            }
        }
    }

    private static WikiLink NewCategoryLink(string target, string? text)
    {
        var newLink = new WikiLink { Target = PlainTextToRun(target) };
        if (text != null)
            newLink.Text = PlainTextToRun(text);

        return newLink;
    }

    private static Run PlainTextToRun(string text)
    {
        return new Run(new PlainText(text));
    }

    private static bool IsItsOwnLine(Node node)
    {
        var prevText = node.PreviousNode?.ToPlainText();
        var nextText = node.NextNode?.ToPlainText();
        return (prevText == null || prevText.EndsWith('\n')) && nextText is "\r\n" or "\n";
    }

    public sealed class CategoryData(Wikitext mainNode, List<WikiLink> categories)
    {
        public Wikitext MainNode { get; } = mainNode;
        public List<WikiLink> Categories { get; } = categories;
    }
}
