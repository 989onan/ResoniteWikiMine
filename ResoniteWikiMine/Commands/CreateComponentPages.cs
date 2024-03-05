using System.Reflection;
using System.Text;
using Dapper;
using Elements.Core;
using FrooxEngine;

namespace ResoniteWikiMine.Commands;

public sealed class CreateComponentPages : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        var excludeCategory = new HashSet<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--exclude":
                    excludeCategory.Add(args[++i]);
                    break;
            }
        }

        var prevRet = WikiComponentReport.RunCoreTransacted(context, []);
        if (!prevRet)
            return 1;

        db.Execute("DELETE FROM wiki_page_create_queue");

        var toCreate = db.Query<(string fullName, string category)>(
            "SELECT full_name, category FROM wiki_component_report WHERE page IS NULL ORDER BY 2, 1");

        var lastCategory = "";
        foreach (var (fullName, category) in toCreate)
        {
            if (excludeCategory.Contains(category))
                continue;

            if (FrooxLoader.FindFrooxType(fullName) is not { } componentType)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unable to find type: {fullName}");
                Console.ResetColor();
                continue;
            }

            if (lastCategory != category)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Category: {category}");
                Console.ResetColor();
                lastCategory = category;
            }

            var title = componentType.Name;
            if (WikiComponentReport.GetTypeWithoutGenericSuffix(componentType.Name) is { } nonGeneric)
                title = nonGeneric;

            title = $"Component:{title}";

            // God save me.
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Component: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(fullName.PadRight(60));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" (page: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(title);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(")");
            Console.ResetColor();

            var text = GenerateWikitext(componentType);

            db.Execute("INSERT INTO wiki_page_create_queue(title, text) VALUES (@Title, @Text)",
                new
                {
                    Title = title,
                    Text = text
                });
        }

        transaction.Commit();

        return 0;
    }

    private static string GenerateWikitext(Type type)
    {
        var name = type.Name;

        var sb = new StringBuilder();
        sb.AppendLine($$$"""
            {{Infobox Component
            |Image={{{name}}}Component.png
            |Name={{{GetNiceName(type)}}}
            }}
            {{stub}}

            == Usage ==
            {{Table ComponentFields
            }}

            == Behavior ==

            == Examples ==

            == See Also ==

            [[Category:ComponentStubs]]
            """);

        // Run generated page through UpdateComponentPages.
        // This ensures consistency of our output, and simplifies the logic in this command.
        return UpdateComponentPages.GenerateNewPageContent(name, type.FullName!, sb.ToString())!.Value.newContent;
    }

    public static List<string> GetComponentCategory(Type type)
    {
        var attribute = type.GetCustomAttribute<CategoryAttribute>();
        if (attribute == null)
            return ["Uncategorized"];

        var categories = new List<string>(attribute.Paths);
        categories.RemoveAll(x => x == "Hidden");
        return categories;
    }

    internal static string GetNiceName(Type type)
    {
        return type.Name.BeautifyName();
    }
}
