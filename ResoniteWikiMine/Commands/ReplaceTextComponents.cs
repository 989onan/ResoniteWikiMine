using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;
using static ResoniteWikiMine.Utility.ComponentBatchUpdater;

namespace ResoniteWikiMine.Commands;

public sealed class ReplaceTextComponents : ICommand
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
            page => GenerateNewPageContent(page.Type, page.Content, args));
    }

    public static BatchUpdatePageResult? GenerateNewPageContent(Type type, string content, string[] args)
    {
        var prevContent = content;
        var changes = PageChanges.None;

        Dictionary<string, string> replace = new();
        string first = "";
        for (var i = 0; i < args.Length; i++)
        {
            if (i % 2 == 0)
            {
                first = args[i];
            }
            else
            {
                replace.Add(first, args[i]);
                first = "";
            }
        }

        if (!first.Equals("")) return null;

        foreach (var str in replace) {
            if (content.Contains(str.Key))
            {
                content = content.Replace(str.Key, str.Value);
            }
        }

        CheckChange(PageChanges.Replace);
        if (changes == PageChanges.None) return null;


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




    [Flags]
    public enum PageChanges
    {
        None = 0,
        Replace = 1 << 0
    }
}
