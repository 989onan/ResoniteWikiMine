using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dapper;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.UIX;
using Microsoft.Data.Sqlite;
using MwParserFromScratch.Nodes;
using ResoniteWikiMine.Utility;

namespace ResoniteWikiMine.Commands;

public sealed partial class WikiComponentReport : ICommand
{
    private static readonly Regex StripGenericSuffixRegex = MyRegex();

    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        if (RunCoreTransacted(context))
        {
            await transaction.CommitAsync();

            return 0;
        }

        return 1;
    }

    public static bool RunCoreTransacted(WorkContext context)
    {
        var db = context.DbConnection;

        db.Execute("""
            DROP TABLE IF EXISTS wiki_component_create;
            DROP TABLE IF EXISTS wiki_component_update_report;

            DROP VIEW IF EXISTS wiki_component_report_view;
            DROP TABLE IF EXISTS wiki_component_report;
            
            DROP VIEW IF EXISTS wiki_all_report_view;
            DROP TABLE IF EXISTS wiki_all_report;

            DROP VIEW IF EXISTS wiki_all_update_report_view;
            DROP TABLE IF EXISTS wiki_all_update_report;

            

            """);



        db.Execute("""
            CREATE TABLE wiki_component_report (
                name TEXT PRIMARY KEY NOT NULL,
                full_name TEXT UNIQUE,
                category TEXT NOT NULL,
                page INT NULL REFERENCES page(id),
                match_type TEXT NULL
            );

            CREATE VIEW wiki_component_report_view AS
            SELECT
                report.name, report.full_name, report.category, page.title, report.match_type
            FROM wiki_component_report report
            LEFT JOIN page ON page.id = report.page
            """);



        db.Execute("""
            
            CREATE TABLE wiki_all_report(
                name TEXT PRIMARY KEY NOT NULL,
                page INT NULL REFERENCES page_all(id)
            );
            
            

            CREATE VIEW wiki_all_report_view AS
            SELECT
                report.name, page.title
            FROM wiki_all_report report
            LEFT JOIN page_all ON page.id = report.page
            """);

        FrooxLoader.InitializeFrooxWorker();

        var components = new List<ComponentEntry>();
        FlattenComponents(WorkerInitializer.ComponentLibrary, "", components);
        components.RemoveAll(t => t.Type.IsAssignableTo(typeof(ProtoFluxNode)));
        components.RemoveAll(t => t.Type.IsAssignableTo(typeof(ProtoFluxEngineProxy)));

        var typeNamesInv = GetOldTypeNamesInverse();

        foreach (var component in components)
        {
            // Console.WriteLine(component);
            var match = MatchWikiPage(component.Type, db, typeNamesInv);

            db.Execute(
                "INSERT OR REPLACE INTO wiki_component_report VALUES (@Name, @Type, @Category, @Page, @MatchType)", new
                {
                    component.Type.Name,
                    Type = component.Type.FullName,
                    component.Category,
                    Page = match?.Item1,
                    MatchType = match?.Item2.ToString()
                });
        }



        foreach (var page in db.Query<(int id, int name_space, string title)>(
            """
            SELECT
            id, namespace, title
            FROM page_all
            """))
        {
            Console.WriteLine(page.title);
            db.Execute(
                "INSERT OR REPLACE INTO wiki_all_report VALUES (@Name, @Page)", new
                {
                    Name = page.title,
                    Page = page.id
                });
        }

        return true;
    }

    private static (int, MatchType)? MatchWikiPage(
        Type componentType,
        SqliteConnection db,
        Dictionary<string, string[]> typeNamesInv)
    {
        var sub = (GetTypeWithoutGenericSuffix(componentType.Name) ?? componentType.Name).Replace("_"," ");
        var match = db.QueryFirstOrDefault<int?>(
            "SELECT id FROM page WHERE title = @Name OR title = 'Component:' || @Name",
            new { Name = sub });
        if (match != null)
            return (match.Value, MatchType.Exact);
        return null;
    }
    public static string? GetTypeWithoutGenericSuffix(string name)
    {
        int index = name.IndexOf('`');
        return index == -1 ? null : name.Substring(0, index);
    }

    private static void FlattenComponents(
        CategoryNode<Type> category,
        string categoryPath,
        List<ComponentEntry> components)
    {
        foreach (var component in category.Elements)
        {
            components.Add(new ComponentEntry(component, categoryPath));
        }

        foreach (var subcategory in category.Subcategories)
        {
            var subPath = categoryPath == "" ? subcategory.Name : $"{categoryPath} / {subcategory.Name}";
            FlattenComponents(subcategory, subPath, components);
        }
    }

    private static Dictionary<string, string[]> GetOldTypeNamesInverse()
    {
        var field = typeof(AssemblyTypeRegistry).GetField(
            "_movedTypes",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var allMoved = new Dictionary<string, Type>();

        foreach (var registry in FrooxLoader.FrooxTypeRegistries)
        {
            var moved = (Dictionary<string, Type>) field.GetValue(registry)!;
            foreach (var (oldName, type) in moved)
            {
                allMoved.Add(oldName, type);
            }
        }

        return allMoved.GroupBy(x => x.Value).ToDictionary(g => g.Key.FullName!, g => g.Select(x => x.Key).ToArray());
    }

    private sealed record ComponentEntry(Type Type, string Category);

    [GeneratedRegex(@"([a-zA-Z_]+)`\d+")]
    private static partial Regex MyRegex();

    public enum MatchType
    {
        Exact,
        NoGenericSuffix,
        OldName
    }
}
