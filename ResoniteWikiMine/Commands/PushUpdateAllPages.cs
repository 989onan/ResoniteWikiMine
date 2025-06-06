using System.Text.Json;
using Dapper;
using ResoniteWikiMine.MediaWiki;

namespace ResoniteWikiMine.Commands;

public sealed class PushUpdateAllPages : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        /*
        if (args.Length != 1)
        {
            Console.WriteLine("ERROR: specify category as single argument");
            return 1;
        }
        */

        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        var csrfToken = await MediawikiApi.GetCsrfToken(context.HttpClient);
        // Console.WriteLine(csrfToken);

        var toUpdate = db.Query<(int page, string name, string pageTitle, string newWikiText, int baseRevision, string changes)>("""
            SELECT
                wcr.page, wcr.name, p.title, wcur.new_text, pc.revision_id, wcur.changes_text
            FROM wiki_all_update_report wcur
            INNER JOIN main.wiki_all_report wcr on wcur.name = wcr.name
            INNER JOIN main.page_all p on wcr.page = p.id
            INNER JOIN main.page_all_content pc on p.id = pc.id
            WHERE diff != ''
            """);

        foreach (var (page, name, pageTitle, text, baseRevision, changes) in toUpdate)
        {
            // God save me.
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Page: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(name);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" (page: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(pageTitle);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(")");
            Console.ResetColor();

            await UpdateAllPage(context, name, page, baseRevision, csrfToken, text, $"Automated: {changes}");
        }

        transaction.Commit();

        return 0;
    }

    private static async Task UpdateAllPage(WorkContext context,
        string name,
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
            new("watchlist", "nochange"),
            new("text", newContent),
            new("summary", summary),
        ]);
        // ReSharper restore ArrangeObjectCreationWhenTypeNotEvident

        var resp = await context.HttpClient.PostAsync(Constants.WikiApiUrl, requestBody);
        resp.EnsureSuccessStatusCode();

        var response = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"Response: {response}");

        if (JsonSerializer.Deserialize<EditResponseWrap>(response) is { edit: { } edit })
        {
            context.DbConnection.Execute(
                "UPDATE page_all_content SET revision_id = @NewRevision, content = @NewContent WHERE id = @Page",
                new
                {
                    Page = page,
                    NewRevision = edit.newrevid,
                    NewContent = newContent
                });

            context.DbConnection.Execute(
                "DELETE FROM wiki_all_update_report where name = @Name",
                new { Name = name });
        }
    }
}
