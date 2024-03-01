using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Dapper;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;

namespace ResoniteWikiMine.Commands;

public class UpdateSiteInfo : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        await db.ExecuteAsync("DROP TABLE IF EXISTS site_namespace");
        await db.ExecuteAsync("""
            CREATE TABLE site_namespace (
                id INT PRIMARY KEY NOT NULL,
                name TEXT NOT NULL UNIQUE
            )
            """);

        const string url = Constants.WikiApiUrl + "?action=query&meta=siteinfo&siprop=namespaces&format=json";
        var response = await context.HttpClient.GetFromJsonAsync<QueryResponse<SiteInfoResponseQuery>>(url);

        foreach (var (_, ns) in response!.Query.Namespaces)
        {
            await db.ExecuteAsync("INSERT INTO site_namespace (id, name) VALUES (@Id, @Name)", ns);
        }

        Console.WriteLine($"Imported {response.Query.Namespaces.Count} namespaces");

        await transaction.CommitAsync();
        return 0;
    }

    private sealed record SiteInfoResponseQuery(Dictionary<string, SiteInfoResponseNamespace> Namespaces);

    private sealed record SiteInfoResponseNamespace(int Id, [property: JsonPropertyName("*")] string Name);
}