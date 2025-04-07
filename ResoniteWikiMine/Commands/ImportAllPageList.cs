using System.Text.Json.Serialization;
using Dapper;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;

namespace ResoniteWikiMine.Commands;

public class ImportAllPageList : ICommand
{

    public static string[] Include = new string[] {
        "Resonite Wiki",
        "MediaWiki",
        "Help",
        "Component",
        "ProtoFlux",
        "Type"
    };

    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();
        foreach (var i in db.Query<(int id, string name)>("""
            SELECT id, name
            FROM site_namespace
            """
            ))
        {
            if (!Include.Contains(i.Item2))
            {
                continue;
            }
            Console.WriteLine("Importing namespace \"" + i.Item2 + "\"");
            await ImportFromUrl(
            context,
            Constants.WikiApiUrl +
            $"?action=query&generator=allpages&gapnamespace={i.Item1}&format=json&prop=revisions&rvslots=main&rvprop=content|ids");

        }
        Console.WriteLine("Importing by Components category");


        await transaction.CommitAsync();
        return 0;
    }

    private static async Task ImportFromUrl(WorkContext context, string url)
    {
        var db = context.DbConnection;

        await foreach (var response in MediawikiApi.MakeContinueEnumerable<GeneratorResponse>(
                           context.HttpClient, url))
        {
            foreach (var page in response.Pages.Values)
            {
                var revision = page.Revisions[0];
                var slot = revision.Slots["main"];

                await db.ExecuteAsync(
                    "INSERT OR REPLACE INTO page_all (id, namespace, title) VALUES (@PageId, @NamespaceId, @Title)",
                    page);
                
                await db.ExecuteAsync(
                    "INSERT OR REPLACE INTO page_all_content (id, slot, model, format, content, revision_id) VALUES (@PageId, 'main', @ContentModel, @ContentFormat, @Content, @RevisionId)",
                    new
                    {
                        page.PageId,
                        slot.ContentModel,
                        slot.ContentFormat,
                        slot.Content,
                        revision.RevisionId
                    });
            }
        }
    }

    private sealed record GeneratorResponse(
        [property: JsonPropertyName("pages")]
        Dictionary<string, GeneratorPage> Pages);

    private sealed record GeneratorPage(
        [property: JsonPropertyName("pageid")] int PageId,
        [property: JsonPropertyName("ns")] int NamespaceId,
        string Title,
        GeneratorRevision[] Revisions);

    private sealed record GeneratorRevision(
        [property: JsonPropertyName("revid")] int RevisionId,
        [property: JsonPropertyName("parentid")]
        int ParentId,
        Dictionary<string, GeneratorRevisionSlot> Slots);

    private sealed record GeneratorRevisionSlot(
        [property: JsonPropertyName("contentmodel")]
        string ContentModel,
        [property: JsonPropertyName("contentformat")]
        string ContentFormat,
        [property: JsonPropertyName("*")]
        string Content);
}
