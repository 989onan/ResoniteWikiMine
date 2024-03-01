using FrooxEngine;
using ResoniteWikiMine.Generation;

namespace ResoniteWikiMine.Commands;

public sealed class CreateTypeFields : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        FrooxLoader.InitializeFrooxWorker();

        var type = WorkerInitializer.Workers.Single(x => x.Name == args[0]);
        Console.WriteLine(FieldFormatter.MakeTypeFieldsTemplate(type));
        return 0;
    }
}
