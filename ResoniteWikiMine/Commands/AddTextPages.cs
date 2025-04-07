using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;
using static ResoniteWikiMine.Utility.ComponentBatchUpdater;

namespace ResoniteWikiMine.Commands;

public sealed class AddTextPages : ICommand
{
    private static readonly (string frooxCategory, string wikiCategory)[] CategoryDefinitions =
    [
        ("", "Components"),
        ("Assets/Materials", "Materials")
    ];

    public async Task<int> Run(WorkContext context, string[] args)
    {
        return UpdateAllPages(
            context,
            _ => true,
            page => GenerateNewPageContent(page.Name, page.Content, args));
    }

    public static BatchUpdatePageResult? GenerateNewPageContent(string pagename, string content, string[] args)
    {
        var prevContent = content;
        var changes = PageChanges.None;

        // Console.WriteLine("Checking page"+pagename);

        if (pagename.Contains(args[1]) && !content.Contains(args[0]) && args.Skip(2).All(o=>(
        !pagename.Contains(o)
        )))
        {
            Console.WriteLine("Added text");
            content = content.Insert(0, args[0]);
        }

        CheckChange(PageChanges.AddText);
        if (changes == PageChanges.None) return null;

        return new BatchUpdatePageResult
        {
            NewContent = content, ChangeDescription = $"Added " + args[0]
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
        AddText = 1 << 0
    }
}
