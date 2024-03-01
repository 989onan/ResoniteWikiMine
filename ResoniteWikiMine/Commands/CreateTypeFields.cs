using FrooxEngine;
using ResoniteWikiMine.Generation;

namespace ResoniteWikiMine.Commands;

public sealed class CreateTypeFields : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        FrooxLoader.InitializeFrooxWorker();

        foreach (var type in WorkerInitializer.Workers.Where(x => x.Name == args[0]))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{type}");
            Console.ResetColor();

            Console.WriteLine(FieldFormatter.MakeTypeFieldsTemplate(type));
        }
        return 0;
    }
}
