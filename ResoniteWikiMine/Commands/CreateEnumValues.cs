using System.Text;
using FrooxEngine.FinalIK;

namespace ResoniteWikiMine.Commands;

public sealed class CreateEnumValues : ICommand
{
    public Task<int> Run(WorkContext context, string[] args)
    {
        // This is so types are available.
        FrooxLoader.InitializeFrooxWorker();

        foreach (var arg in args)
        {
            Console.WriteLine(typeof(IKSolverVR.Arm.ShoulderRotationMode));
            var type = GetType(arg);
            if (type == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unable to find type: {arg}");
                Console.ResetColor();
                continue;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(arg);
            Console.ResetColor();

            var sb = new StringBuilder();
            sb.AppendLine("{{Table EnumValues");

            foreach (var name in Enum.GetNames(type).OrderBy(name => Enum.Parse(type, name)))
            {
                var value = (int) Enum.Parse(type, name);
                sb.AppendLine($"|{name}|{value}|");
            }

            sb.AppendLine("}}");

            Console.WriteLine(sb.ToString());
        }

        return Task.FromResult(0);
    }

    private static Type? GetType(string name)
    {
        foreach (var assembly in FrooxLoader.FrooxAssemblies)
        {
            if (assembly.GetType(name) is { } type)
                return type;
        }


        return null;
    }
}
