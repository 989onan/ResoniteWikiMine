using System.Reflection;
using System.Text;
using Elements.Core;
using FrooxEngine;
using ResoniteWikiMine.Generation;

namespace ResoniteWikiMine.Commands;

public sealed class CreateComponentPages : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        FrooxLoader.InitializeFrooxWorker();

        foreach (var componentName in args)
        {
            var component = WorkerInitializer.Workers.Single(x => x.Name == componentName);
            Console.WriteLine(component);

            var text = GenerateWikitext(component);
            Console.WriteLine(text);
        }

        return 0;
    }

    private static string GenerateWikitext(Type type)
    {
        var name = type.Name;

        var sb = new StringBuilder();
        sb.AppendLine($$$"""
            <languages></languages>
            <translate>
            {{stub}}
            {{Infobox Component
            |Image={{{name}}}Component.png
            |Name={{{GetNiceName(type)}}}
            }}

            == Fields ==
            """);

        sb.AppendLine(FieldFormatter.MakeComponentFieldsTemplate(type));

        sb.AppendLine("""
            == Behavior ==

            == Examples ==

            == Related Components ==
            </translate>
            [[Category:ComponentStubs]]
            [[Category:Components{{#translation:}}]]
            """);

        foreach (var category in GetComponentCategory(type))
        {
            sb.AppendLine($"[[Category:Components:{category.Replace('/', ':')}{{{{#translation:}}}}]]");
        }

        return sb.ToString();
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
