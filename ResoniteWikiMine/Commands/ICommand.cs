using Microsoft.Data.Sqlite;

namespace ResoniteWikiMine.Commands;

public interface ICommand
{
    Task<int> Run(WorkContext context, string[] args);
}

public sealed class WorkContext(HttpClient httpClient, SqliteConnection dbConnection)
{
    public HttpClient HttpClient { get; } = httpClient;
    public SqliteConnection DbConnection { get; } = dbConnection;
}
