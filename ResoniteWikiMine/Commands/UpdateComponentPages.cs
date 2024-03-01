using Dapper;
using FrooxEngine;
using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using ResoniteWikiMine.Utility;

namespace ResoniteWikiMine.Commands;

public sealed class UpdateComponentPages : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        var prevRet = await new WikiComponentReport().Run(context, Array.Empty<string>());
        if (prevRet != 0)
            return prevRet;

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Start UpdateComponentPages");
        Console.ResetColor();

        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        db.Execute("DROP VIEW IF EXISTS wiki_component_update_report_view");
        db.Execute("DROP TABLE IF EXISTS wiki_component_update_report");
        db.Execute("""
            CREATE TABLE wiki_component_update_report (
                name TEXT PRIMARY KEY NOT NULL REFERENCES wiki_component_report(name),
                new_text TEXT NOT NULL,
                diff TEXT NOT NULL
            );
            """);

        db.Execute("""
            CREATE VIEW wiki_component_update_report_view AS
            SELECT wcur.name, wcr.category, pc.content old_text, wcur.new_text, wcur.diff
            FROM wiki_component_update_report wcur
            INNER JOIN wiki_component_report wcr ON wcur.name = wcr.name
            INNER JOIN page_content pc ON pc.id = wcr.page
            WHERE wcur.diff != ''
            ORDER BY 1
            """);

        var components = db.Query<(string name, string fullName, string content)>("""
            SELECT
                wcr.name, wcr.full_name, pc.content
            FROM wiki_component_report wcr
            INNER JOIN main.page_content pc ON wcr.page = pc.id AND pc.slot = 'main'
            ORDER BY 1
            """);

        foreach (var (name, fullname, content) in components)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Component: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(name);
            Console.ResetColor();

            try
            {
                var newContent = GenerateNewPageContent(name, fullname, content);
                if (newContent == null)
                    continue;

                var diff = DiffFormatter.GenerateDiff(content, newContent);

                db.Execute(
                    "INSERT INTO wiki_component_update_report (name, new_text, diff) VALUES (@Name, @NewContent, @Diff)",
                    new
                    {
                        Name = name,
                        NewContent = newContent,
                        Diff = diff
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while processing {name}: {e}");
            }
        }

        await transaction.CommitAsync();

        return 0;
    }

    private static string? GenerateNewPageContent(string name, string fullname, string content)
    {
        var fieldsTemplate = PageContentParser.GetTemplateInPage(content, "Table ComponentFields");
        if (fieldsTemplate == null)
        {
            Console.WriteLine($"Unable to find Table ComponentFields in page for {name}");
            return null;
        }

        var type = WorkerManager.GetType(fullname);
        if (type == null)
        {
            Console.WriteLine($"Unable to find .NET type for {name} ???");
            return null;
        }

        var fieldDescriptions = ParseComponentFields(fieldsTemplate);

        return SpliceString(
            content,
            fieldsTemplate.Range,
            FieldFormatter.MakeComponentFieldsTemplate(type, fieldDescriptions));
    }

    private static Dictionary<string, string> ParseComponentFields(PageContentParser.TemplateMatch template)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var args = template.PositionalArguments;
        for (var i = 0; i < args.Length; i += 3)
        {
            var name = template.PositionalArguments[i];
            var description = template.PositionalArguments[i + 2].TrimEnd();
            if (!fields.TryAdd(name, description))
                Console.WriteLine($"Duplicate property: {name}");
        }

        return fields;
    }

    private static string SpliceString(string text, Range range, string replacement)
    {
        return string.Concat(
            text.AsSpan(0, range.Start.GetOffset(text.Length)),
            replacement,
            text.AsSpan(range.End.GetOffset(text.Length)));
    }
}