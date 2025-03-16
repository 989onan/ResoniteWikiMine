using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;
using static ResoniteWikiMine.Utility.ComponentBatchUpdater;

namespace ResoniteWikiMine.Commands;

public sealed class UpdateComponentPages : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        return UpdateComponentPages(
            context,
            _ => true,
            page => UpdateComponentPage.GenerateNewPageContent(page.Type, page.Content));
    }
}
