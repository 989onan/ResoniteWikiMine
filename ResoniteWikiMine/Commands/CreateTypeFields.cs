using FrooxEngine;
using ResoniteWikiMine.Generation;

namespace ResoniteWikiMine.Commands;

public sealed class CreateTypeFields : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        FrooxLoader.InitializeFrooxWorker();

        Type? containing = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--containing":
                    var containingName = args[++i];
                    containing = WorkerInitializer.Workers.FirstOrDefault(x => x.Name == containingName);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Nested container: {containing}");
                    Console.ResetColor();
                    break;
                default:
                    foreach (var type in WorkerInitializer.Workers.Where(x => x.Name == args[i]))
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"{type}");
                        Console.ResetColor();

                        Console.WriteLine(FieldFormatter.MakeTypeFieldsTemplate(type, containingType: containing));
                    }
                    break;
            }
        }

        return 0;
    }
}
