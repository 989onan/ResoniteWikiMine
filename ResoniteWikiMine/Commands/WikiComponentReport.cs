using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using Microsoft.Data.Sqlite;

namespace ResoniteWikiMine.Commands;

public sealed partial class WikiComponentReport : ICommand
{
    private static readonly Regex StripGenericSuffixRegex = MyRegex();

    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        await db.ExecuteAsync("""
            DROP TABLE IF EXISTS wiki_component_update_report;
            DROP VIEW IF EXISTS wiki_component_report_view;
            DROP TABLE IF EXISTS wiki_component_report;

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

        FrooxLoader.InitializeFrooxWorker();

        var components = new List<ComponentEntry>();
        FlattenComponents(WorkerInitializer.ComponentLibrary, "", components);
        components.RemoveAll(t => t.Type.IsAssignableTo(typeof(ProtoFluxNode)));

        var typeNamesInv = GetOldTypeNamesInverse();

        foreach (var component in components)
        {
            // Console.WriteLine(component);
            var match = MatchWikiPage(component.Type, db, typeNamesInv);

            await db.ExecuteAsync(
                "INSERT OR REPLACE INTO wiki_component_report VALUES (@Name, @Type, @Category, @Page, @MatchType)", new
                {
                    component.Type.Name,
                    Type = component.Type.FullName,
                    component.Category,
                    Page = match?.Item1,
                    MatchType = match?.Item2.ToString()
                });
        }

        await transaction.CommitAsync();

        return 0;
    }

    private static (int, MatchType)? MatchWikiPage(
        Type componentType,
        SqliteConnection db,
        Dictionary<string, string[]> typeNamesInv)
    {
        var candidates = new List<(string, MatchType)> { (componentType.Name, MatchType.Exact) };
        if (typeNamesInv.TryGetValue(componentType.FullName!, out var oldNames))
        {
            foreach (var oldName in oldNames)
            {
                candidates.Add((oldName.Split('.')[^1], MatchType.OldName));
            }
        }

        foreach (var (candidate, matchType) in candidates.ToArray())
        {
            if (GetTypeWithoutGenericSuffix(candidate) is { } nonGeneric)
            {
                candidates.Add((nonGeneric, matchType == MatchType.Exact ? MatchType.NoGenericSuffix : MatchType.OldName));
            }
        }

        foreach (var (candidate, matchType) in candidates)
        {
            var sub = candidate.Replace('_', ' ');
            var match = db.QueryFirstOrDefault<int?>(
                "SELECT id FROM page WHERE title = @Name OR title = 'Component:' || @Name",
                new { Name = sub });
            if (match != null)
                return (match.Value, matchType);
        }

        return null;
    }

    private static string? GetTypeWithoutGenericSuffix(string name)
    {
        var match = StripGenericSuffixRegex.Match(name);
        if (match.Success)
            return match.Groups[1].Value;
        return null;
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
        var table = (Dictionary<string, string>)typeof(WorkerInitializer)
            .GetField("oldTypenames", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

        return table.GroupBy(x => x.Value).ToDictionary(g => g.Key, g => g.Select(x => x.Key).ToArray());
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