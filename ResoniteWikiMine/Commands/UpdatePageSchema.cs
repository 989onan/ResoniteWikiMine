using Dapper;

namespace ResoniteWikiMine.Commands;

public class UpdatePageSchema : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        db.Execute("DROP VIEW IF EXISTS wiki_component_update_report_view");
        db.Execute("DROP TABLE IF EXISTS wiki_component_update_report");

        db.Execute("DROP VIEW IF EXISTS wiki_all_update_report_view");
        db.Execute("DROP TABLE IF EXISTS wiki_all_update_report");
        await db.ExecuteAsync("DROP VIEW IF EXISTS wiki_component_report_view");
        await db.ExecuteAsync("DROP VIEW IF EXISTS wiki_all_page_report");
        await db.ExecuteAsync("DROP TABLE IF EXISTS wiki_component_report");
        await db.ExecuteAsync("DROP TABLE IF EXISTS page_content");
        await db.ExecuteAsync("DROP TABLE IF EXISTS page");

        await db.ExecuteAsync("DROP TABLE IF EXISTS page_all_content");
        await db.ExecuteAsync("DROP TABLE IF EXISTS page_all");

        await db.ExecuteAsync("""
            CREATE TABLE page (
                id INT PRIMARY KEY NOT NULL,
                namespace INT NOT NULL REFERENCES site_namespace(id),
                title TEXT NOT NULL UNIQUE
            )
            """);

        await db.ExecuteAsync("""
            CREATE TABLE page_all (
                id INT PRIMARY KEY NOT NULL,
                namespace INT NOT NULL REFERENCES site_namespace(id),
                title TEXT NOT NULL UNIQUE
            )
            """);

        await db.ExecuteAsync("""
            CREATE TABLE page_content (
                id INT NOT NULL REFERENCES page(id),
                slot TEXT NOT NULL,
                model TEXT NOT NULL,
                format TEXT NOT NULL,
                content TEXT NOT NULL,
                revision_id INT NOT NULL,
                PRIMARY KEY (id, slot)
            )
            """);

        await db.ExecuteAsync("""
            CREATE TABLE page_all_content (
                id INT NOT NULL REFERENCES page_all(id),
                slot TEXT NOT NULL,
                model TEXT NOT NULL,
                format TEXT NOT NULL,
                content TEXT NOT NULL,
                revision_id INT NOT NULL,
                PRIMARY KEY (id, slot)
            )
            """);


        db.Execute("""
            DROP TABLE IF EXISTS wiki_page_create_queue;
            CREATE TABLE wiki_page_create_queue (
                title TEXT PRIMARY KEY NOT NULL,
                text TEXT NOT NULL
            );
            """);

        await transaction.CommitAsync();

        return 0;
    }
}
