using System.Reflection;
using System.Text;
using Dapper;
using Elements.Core;
using FrooxEngine;
using ResoniteWikiMine.MediaWiki;

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

        var prevRet = WikiComponentReport.RunCoreTransacted(context);
        if (!prevRet)
            return 1;

        db.Execute("DELETE FROM wiki_page_create_queue");

        var toCreate = db.Query<(string name, string fullName, string category, string page)>(
            "SELECT name, full_name, category, page FROM wiki_component_report ORDER BY 2, 1");

        var pages = db.Query<(int id, string title)>(
            "SELECT id, title FROM page");

        var page_content = db.Query<(int id, string content)>(
            "SELECT id, content FROM page_content");

        var lastCategory = "";
        foreach (var (name, fullName, category, page) in toCreate)
        {
            if (excludeCategory.Contains(category))
                continue;

            Type? componentType = FrooxLoader.FindFrooxType(fullName);
            if (componentType == null)
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
            string ComponentTypeName = componentType.Name.Replace("_", " ");
            var title = componentType.Name.Replace("_", " ");
            var text = "";
            string? nonGeneric = WikiComponentReport.GetTypeWithoutGenericSuffix(ComponentTypeName);

            if (nonGeneric != null)
            {
                title = nonGeneric;
                var h2 = pages.ToList().Find(o => o.title == $"Component:{ComponentTypeName}");
                var j2 = page_content.ToList().Find(o => h2.id == o.id);
                if (j2.content == null || j2.content.Equals(""))
                {
                    title = $"Component:{ComponentTypeName}";
                    text = $"#REDIRECT[[Component:{nonGeneric}]]";
                    goto db_insert;

                }
            }

            var h = pages.ToList().Find(o => o.title == $"Component:{title}");
            var j = page_content.ToList().Find(o => h.id == o.id);
            if (j.content == null || j.content.Equals(""))
            {
                text = GenerateWikitext(componentType);
                title = $"Component:{title}";
            }
            else
            {
                continue;
            }



        db_insert:
            // God save me.
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Creating Component page in database: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(fullName.PadRight(60));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" (page: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(title);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(")");
            Console.ResetColor();

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
            The '''{{{GetNiceName(type)}}}''' component
            {{stub}}

            == Fields ==
            {{Table ComponentFields
            }}

            == Usage ==

            == Examples ==

            == See Also ==

            [[Category:ComponentStubs]]
            """);

        // Run generated page through UpdateComponentPages.
        // This ensures consistency of our output, and simplifies the logic in this command.
        return UpdateComponentPage.GenerateNewPageContent(type, sb.ToString())!.NewContent;
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



    public static string GetNiceName(Type type)
    {
        return type.Name.BeautifyName();
    }
}
