using System.Net;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;

namespace ResoniteWikiMine.Commands;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface ICommand
{
    Task<int> Run(WorkContext context, string[] args);
}

public sealed class WorkContext(
    CookieContainer cookieContainer,
    HttpClient httpClient,
    SqliteConnection dbConnection,
    SqliteConnection authDbConnection)
{
    public CookieContainer CookieContainer { get; } = cookieContainer;
    public HttpClient HttpClient { get; } = httpClient;
    public SqliteConnection DbConnection { get; } = dbConnection;
    public SqliteConnection AuthDbConnection { get; } = authDbConnection;
}
