using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace ResoniteWikiMine.MediaWiki;

/// <summary>
/// Helper class for working with page contents.
/// </summary>
public static class PageContentParser
{
    /// <summary>
    /// Locate a template in a wikitext page.
    /// </summary>
    /// <param name="wikitext">The wikitext to search through.</param>
    /// <param name="templateName">The name of the template to find.</param>
    /// <returns>An object that represents the matched template.</returns>
    public static TemplateMatch? GetTemplateInPage(string wikitext, string templateName)
    {
        var parser = new WikitextParser();
        var parseResult = parser.Parse(wikitext);
        foreach (var template in parseResult.EnumDescendants().OfType<Template>())
        {
            if (!TemplateNameMatches(template.Name.ToPlainText(), templateName))
                continue;

            var args = new List<string>();
            var named = new Dictionary<string, string>();

            foreach (var argument in template.Arguments)
            {
                var value = argument.Value.ToString();
                if (argument.Name == null)
                    args.Add(value);
                else
                    named[argument.Name.ToString()] = value;
            }

            var range = WikiTextToRange(wikitext, template);
            return new TemplateMatch(args.ToArray(), named, range);
        }

        return null;
    }

    private static Range WikiTextToRange(string wikitext, IWikitextLineInfo info)
    {
        return new Range(
            new Index(LineToIndex(wikitext, info.StartLineNumber) + info.StartLinePosition),
            new Index(LineToIndex(wikitext, info.EndLineNumber) + info.EndLinePosition));
    }

    private static int LineToIndex(string wikitext, int line)
    {
        var index = 0;
        var curLine = 0;
        while (curLine < line)
        {
            var newLineIndex = wikitext.IndexOf('\n', index);
            if (newLineIndex == -1)
                throw new InvalidOperationException("Unable to find line!");

            index = newLineIndex + 1;
            curLine += 1;
        }

        return index;
    }

    private static bool TemplateNameMatches(ReadOnlySpan<char> wikiTextName, string wantedName)
    {
        return wikiTextName.Trim().SequenceEqual(wantedName);
    }

    /// <summary>
    /// Represents a template matched in a WikiText page.
    /// </summary>
    /// <param name="PositionalArguments">The positional arguments to the template.</param>
    /// <param name="NamedArguments">The named arguments to the template.</param>
    /// <param name="Range">
    /// The range in the original page text that the template appears at.
    /// This includes the opening <c>{{</c> and the closing <c>}}</c>.
    /// </param>
    public sealed record TemplateMatch(
        string[] PositionalArguments,
        Dictionary<string, string> NamedArguments,
        Range Range);
}
