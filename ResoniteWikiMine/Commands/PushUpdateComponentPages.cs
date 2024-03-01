using System.Net.Http.Json;
using Dapper;
using ResoniteWikiMine.MediaWiki;

namespace ResoniteWikiMine.Commands;

public sealed class PushUpdateComponentPages : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("ERROR: specify category as single argument");
            return 1;
        }

        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        var csrfToken = await GetCsrfToken(context.HttpClient);
        Console.WriteLine(csrfToken);

        var toUpdate = db.Query<(int page, string name, string pageTitle, string newWikiText, int baseRevision)>("""
            SELECT
                wcr.page, wcr.name, p.title, wcur.new_text, pc.revision_id
            FROM wiki_component_update_report wcur
            INNER JOIN main.wiki_component_report wcr on wcur.name = wcr.name
            INNER JOIN main.page p on wcr.page = p.id
            INNER JOIN main.page_content pc on p.id = pc.id
            WHERE wcr.category = @Category AND diff != ''
            """,
            new { Category = args[0] });

        foreach (var (page, name, pageTitle, text, baseRevision) in toUpdate)
        {
            // God save me.
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Component: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(name);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" (page: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(pageTitle);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(")");
            Console.ResetColor();

            await UpdatePage(context, page, baseRevision, csrfToken, text, "Automated: update component fields");
        }

        return 0;
    }

    private static async Task<string> GetCsrfToken(HttpClient http)
    {
        const string url = Constants.WikiApiUrl + "?action=query&format=json&meta=tokens";

        var response = await http.GetFromJsonAsync<QueryResponse<TokenResponse>>(url);
        return response!.Query.Tokens["csrftoken"];
    }

    private static async Task UpdatePage(
        WorkContext context,
        int page,
        int baseRevision,
        string csrfToken,
        string newContent,
        string summary)
    {
        // ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
        var requestBody = new FormUrlEncodedContent([
            new("action", "edit"),
            new("pageid", page.ToString()),
            new("token", csrfToken),
            new("format", "json"),
            new("baserevid", baseRevision.ToString()),
            new("bot", "true"),
            new("contentformat", "text/x-wiki"),
            new("contentmodel", "wikitext"),
            new("text", newContent),
            new("summary", summary),
        ]);
        // ReSharper restore ArrangeObjectCreationWhenTypeNotEvident

        var resp = await context.HttpClient.PostAsync(Constants.WikiApiUrl, requestBody);
        resp.EnsureSuccessStatusCode();

        Console.WriteLine(await resp.Content.ReadAsStringAsync());

        // var editResponse = await resp.Content.ReadFromJsonAsync<EditResponseWrap>();
        // Console.Write($"Status: {editResponse!.Edit}");
    }

    private sealed record TokenResponse(Dictionary<string, string> Tokens);

    private sealed record EditResponseWrap(EditResponse Edit);

    private sealed record EditResponse(string Result);
}
