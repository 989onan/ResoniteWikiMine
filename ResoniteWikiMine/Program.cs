using System.Net;
using Dapper;
using Microsoft.Data.Sqlite;
using ResoniteWikiMine;
using ResoniteWikiMine.Commands;

if (args.Length < 1)
{
    Console.WriteLine("Pass a command, nerd");
    return 1;
}

var commandType = typeof(Program).Assembly.DefinedTypes.
    SingleOrDefault(x => x.IsAssignableTo(typeof(ICommand)) && x.Name == args[0] && !x.IsAbstract);

if (commandType == null)
{
    Console.WriteLine($"Unknown command: {args[0]}");
    return 1;
}

var cookieContainer = new CookieContainer();

using var httpClient = new HttpClient(new SocketsHttpHandler { CookieContainer = cookieContainer });
httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent);

using var dbConnection = new SqliteConnection($"Data Source={Constants.DbName}");
dbConnection.Open();

using var authDbConnection = new SqliteConnection($"Data Source={Constants.AuthDbName}");
authDbConnection.Open();

var workContext = new WorkContext(cookieContainer, httpClient, dbConnection, authDbConnection);

LoadCookiesFromDb(workContext);

var command = (ICommand) Activator.CreateInstance(commandType)!;
return await command.Run(workContext, args[1..]);

void LoadCookiesFromDb(WorkContext context)
{
    using var tx = context.AuthDbConnection.BeginTransaction();
    var hasCookies = context.AuthDbConnection.QuerySingle<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='cookies'");

    if (hasCookies == 0)
        return;

    var cookies = context.AuthDbConnection.Query<(string, string, string, string)>("""
        SELECT name, path, domain, value
        FROM cookies
        """);

    foreach (var (name, path, domain, value) in cookies)
    {
        var cookie = new Cookie(name, value, path, domain);
        context.CookieContainer.Add(cookie);
    }
}
