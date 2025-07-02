using System.Text.Json;
using Dapper;
using ResoniteWikiMine.MediaWiki;

namespace ResoniteWikiMine.Commands;

public sealed class CreateQueuedPages : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        using var transaction = db.BeginTransaction();

        var csrfToken = "";

        const int pagesPerToken = 20;
        var csrfCounter = pagesPerToken + 1;

        var queue = db.Query<(string title, string text)>("SELECT title, text FROM wiki_page_create_queue ORDER BY 1");
        foreach (var (title, text) in queue)
        {
            if (++csrfCounter > pagesPerToken)
            {
                Console.WriteLine("Getting new CSRF token.");
                csrfToken = await MediawikiApi.GetCsrfToken(context.HttpClient);
                csrfCounter = 0;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Page: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(title);
            Console.ResetColor();

            await CreatePage(context, title, text, "Automated: create new component page", csrfToken);
        }

        return 0;
    }

    private static async Task CreatePage(
        WorkContext context,
        string title,
        string content,
        string summary,
        string csrfToken)
    {
        // ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
        var requestBody = new FormUrlEncodedContent([
            new("action", "edit"),
            new("title", title),
            new("token", csrfToken),
            new("format", "json"),
            new("bot", "true"),
            new("createonly", "true"),
            new("contentformat", "text/x-wiki"),
            new("contentmodel", "wikitext"),
            new("watchlist", "nochange"),
            new("text", content),
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
                "DELETE FROM wiki_page_create_queue where title = @Title",
                new { Title = title });
        }
    }
}
