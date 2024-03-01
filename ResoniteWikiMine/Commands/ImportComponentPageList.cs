using System.Text.Json.Serialization;
using Dapper;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;

namespace ResoniteWikiMine.Commands;

public class ImportComponentPageList : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        const string url = Constants.WikiApiUrl +
                           "?action=query&generator=categorymembers&gcmtitle=Category:Components&format=json&prop=revisions&rvslots=main&rvprop=content";

        await foreach (var response in MediawikiApi.MakeContinueEnumerable<CategoryMembersResponse>(
                           context.HttpClient, url))
        {
            foreach (var page in response.Pages.Values)
            {
                var revision = page.Revisions[0];
                var slot = revision.Slots["main"];

                await db.ExecuteAsync(
                    "INSERT OR REPLACE INTO page (id, namespace, title) VALUES (@PageId, @NamespaceId, @Title)",
                    page);

                await db.ExecuteAsync(
                    "INSERT OR REPLACE INTO page_content (id, slot, model, format, content) VALUES (@PageId, 'main', @ContentModel, @ContentFormat, @Content)",
                    new
                    {
                        page.PageId,
                        slot.ContentModel,
                        slot.ContentFormat,
                        slot.Content
                    });
            }

            await Task.Delay(15);
        }

        await transaction.CommitAsync();

        return 0;
    }

    private sealed record CategoryMembersResponse(
        [property: JsonPropertyName("pages")]
        Dictionary<string, CategoryMembersPage> Pages);

    private sealed record CategoryMembersPage(
        [property: JsonPropertyName("pageid")] int PageId,
        [property: JsonPropertyName("ns")] int NamespaceId,
        string Title,
        CategoryMembersRevision[] Revisions);

    private sealed record CategoryMembersRevision(
        Dictionary<string, CategoryMembersRevisionSlot> Slots);

    private sealed record CategoryMembersRevisionSlot(
        [property: JsonPropertyName("contentmodel")]
        string ContentModel,
        [property: JsonPropertyName("contentformat")]
        string ContentFormat,
        [property: JsonPropertyName("*")]
        string Content);
}