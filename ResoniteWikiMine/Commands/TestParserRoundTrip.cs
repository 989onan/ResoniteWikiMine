using Dapper;
using MwParserFromScratch;
using ResoniteWikiMine.Utility;

namespace ResoniteWikiMine.Commands;

public sealed class TestParserRoundTrip : ICommand
{
    public Task<int> Run(WorkContext context, string[] args)
    {
        using var tx = context.DbConnection.BeginTransaction();

        var allPages = context.DbConnection.Query<(string title, string content)>("""
            SELECT p.title, pc.content FROM page_content pc
            INNER JOIN page p ON pc.id = p.id
            """);

        var parser = new WikitextParser();
        foreach (var (title, contents) in allPages)
        {
            var parsed = parser.Parse(contents);
            var output = parsed.ToString();
            if (contents != output)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{title} DIFFERS:");
                Console.ResetColor();
                Console.WriteLine(DiffFormatter.GenerateDiff(contents, output));
            }
        }

        return Task.FromResult(0);
    }
}
