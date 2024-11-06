using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;
using static ResoniteWikiMine.Utility.ComponentBatchUpdater;

namespace ResoniteWikiMine.Commands;

public sealed class RemoveDuplicatesComponents : ICommand
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

        foreach (string str in args)
        {
            if (content.Contains(str))
            {
                int index = content.IndexOf(str);
                string replaced = (index < 0)
                    ? content
                    : content.Remove(index, str.Length);
                if (replaced.Contains(str))
                {
                    content = replaced;
                }
            }
        }

        CheckChange(PageChanges.DeDuplicate);
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
        DeDuplicate = 1 << 0
    }
}
