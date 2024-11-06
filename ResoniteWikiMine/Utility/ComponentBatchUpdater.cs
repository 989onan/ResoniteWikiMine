using Dapper;
using Microsoft.Data.Sqlite;
using ResoniteWikiMine.Commands;

namespace ResoniteWikiMine.Utility;

public static class ComponentBatchUpdater
{
    public static int UpdateComponentPages(
        WorkContext context,
        Func<BatchUpdatePage, bool> isEligible,
        Func<BatchUpdatePage, BatchUpdatePageResult?> processor)
    {
        var db = context.DbConnection;
        using var transaction = db.BeginTransaction();

        var prevRet = WikiComponentReport.RunCoreTransacted(context);
        if (!prevRet)
            return 1;

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Start batch component update");
        Console.ResetColor();

        EnsureDbTables(db);

        var components = db.Query<(string name, string fullName, string content)>("""
            SELECT
                wcr.name, wcr.full_name, pc.content
            FROM wiki_component_report wcr
            INNER JOIN main.page_content pc ON wcr.page = pc.id AND pc.slot = 'main'
            ORDER BY 1
            """);

        foreach (var (name, fullname, content) in components)
        {
            var type = FrooxLoader.GetType(fullname);
            if (type == null)
            {
                Console.WriteLine($"Unable to find .NET type for {name} ???");
                continue;
            }

            var pageObject = new BatchUpdatePage
            {
                Name = name, Type = type, Content = content
            };

            if (!isEligible(pageObject))
                continue;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Component: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(name);
            Console.ResetColor();

            try
            {
                var newContent = processor(pageObject);
                if (newContent == null || newContent.NewContent == pageObject.Content)
                    continue;

                var diff = DiffFormatter.GenerateDiff(content, newContent.NewContent);

                db.Execute(
                    "INSERT INTO wiki_component_update_report (name, new_text, diff, changes_text) " +
                    "VALUES (@Name, @NewContent, @Diff, @ChangesText)",
                    new
                    {
                        Name = name,
                        newContent.NewContent,
                        Diff = diff,
                        ChangesText = newContent.ChangeDescription
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while processing {name}: {e}");
            }
        }

        transaction.Commit();

        return 0;
    }

    public static int UpdateComponentPage(
        WorkContext context,
        Func<BatchUpdatePage, bool> isEligible,
        Func<BatchUpdatePage, BatchUpdatePageResult?> processor, string compname)
    {
        var db = context.DbConnection;
        using var transaction = db.BeginTransaction();

        var prevRet = WikiComponentReport.RunCoreTransacted(context);
        if (!prevRet)
            return 1;

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Start batch component update");
        Console.ResetColor();

        EnsureDbTables(db);

        var components = db.Query<(string name, string fullName, string content)>("""
            SELECT
                wcr.name, wcr.full_name, pc.content
            FROM wiki_component_report wcr
            INNER JOIN main.page_content pc ON wcr.page = pc.id AND pc.slot = 'main'
            ORDER BY 1
            """);

        var (name, fullname, content) = components.Where(o => o.name == compname).First();

        var type = FrooxLoader.GetType(fullname);
        if (type == null)
        {
            Console.WriteLine($"Unable to find .NET type for {name} ???");
            return 1;
        }

        var pageObject = new BatchUpdatePage
        {
            Name = name, Type = type, Content = content
        };

        if (!isEligible(pageObject)) return 1;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Component: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(name);
        Console.ResetColor();

        try
        {
            var newContent = processor(pageObject);
            if (newContent == null || newContent.NewContent == pageObject.Content)
                return 1;

            var diff = DiffFormatter.GenerateDiff(content, newContent.NewContent);

            db.Execute(
                "INSERT INTO wiki_component_update_report (name, new_text, diff, changes_text) " +
                "VALUES (@Name, @NewContent, @Diff, @ChangesText)",
                new
                {
                    Name = name,
                    newContent.NewContent,
                    Diff = diff,
                    ChangesText = newContent.ChangeDescription
                });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error while processing {name}: {e}");
        }

        transaction.Commit();

        return 0;
    }

    private static void EnsureDbTables(SqliteConnection db)
    {
        db.Execute("DROP VIEW IF EXISTS wiki_component_update_report_view");
        db.Execute("DROP TABLE IF EXISTS wiki_component_update_report");
        db.Execute("""
            CREATE TABLE wiki_component_update_report (
                name TEXT PRIMARY KEY NOT NULL REFERENCES wiki_component_report(name),
                new_text TEXT NOT NULL,
                changes_text TEXT NOT NULL,
                diff TEXT NOT NULL
            );
            """);

        db.Execute("""
            CREATE VIEW wiki_component_update_report_view AS
            SELECT wcur.name, wcr.category, pc.content old_text, wcur.new_text, wcur.changes_text, wcur.diff
            FROM wiki_component_update_report wcur
            INNER JOIN wiki_component_report wcr ON wcur.name = wcr.name
            INNER JOIN page_content pc ON pc.id = wcr.page
            WHERE wcur.diff != ''
            ORDER BY 1
            """);
    }

    public sealed class BatchUpdatePage
    {
        public required Type Type;
        public required string Name;
        public required string Content;
    }

    public sealed class BatchUpdatePageResult
    {
        public required string NewContent;
        public required string ChangeDescription;
    }
}
