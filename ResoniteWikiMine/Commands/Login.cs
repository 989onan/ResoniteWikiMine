using System.Net.Http.Json;
using Dapper;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;

namespace ResoniteWikiMine.Commands;

public sealed class Login : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        using var authTx = context.AuthDbConnection.BeginTransaction();

        InitializeCookieDatabase(context);

        Console.WriteLine("Fetching login token...");
        var loginToken = await FetchLoginToken(context.HttpClient);

        Console.Write("Username? ");
        var username = Console.ReadLine();

        Console.Write("Password? ");
        ConsoleHelper.SetInputEchoEnabled(false);
        var password = Console.ReadLine();
        ConsoleHelper.SetInputEchoEnabled(true);
        Console.WriteLine();

        // ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
        var requestBody = new FormUrlEncodedContent([
            new("action", "clientlogin"),
            new("username", username),
            new("password", password),
            new("rememberMe", "true"),
            new("loginreturnurl", "https://localhost"),
            new("logintoken", loginToken),
            new("format", "json")
        ]);
        // ReSharper restore ArrangeObjectCreationWhenTypeNotEvident

        var response = await context.HttpClient.PostAsync(Constants.WikiApiUrl, requestBody);

        Console.WriteLine(await response.Content.ReadAsStringAsync());

        foreach (var cookie in context.CookieContainer.GetAllCookies())
        {
            context.AuthDbConnection.Execute("INSERT INTO cookies VALUES (@Name, @Path, @Domain, @Value)", cookie);
        }

        authTx.Commit();

        return 0;
    }

    private static async Task<string> FetchLoginToken(HttpClient httpClient)
    {
        const string url = Constants.WikiApiUrl + "?action=query&meta=tokens&type=login&format=json";

        var response = await httpClient.GetFromJsonAsync<QueryResponse<TokenResponse>>(url);
        return response!.Query.Tokens["logintoken"];
    }

    private sealed record TokenResponse(Dictionary<string, string> Tokens);

    private static void InitializeCookieDatabase(WorkContext context)
    {
        context.AuthDbConnection.Execute("""
            DROP TABLE IF EXISTS cookies;
            CREATE TABLE cookies(
                name TEXT NOT NULL,
                path TEXT NOT NULL,
                domain TEXT NOT NULL,
                value TEXT NOT NULL
            );
            """);
    }
}
