using System.Reflection;
using FrooxEngine;

namespace ResoniteWikiMine;

public static class FrooxLoader
{
    private static bool _isFrooxWorkerInitialized;

    public static void InitializeFrooxWorker()
    {
        if (_isFrooxWorkerInitialized)
            return;

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Loading Froox");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        PreloadFrooxAssemblies();

        WorkerInitializer.Initialize(GetAllFrooxTypes(), true);
        Console.ResetColor();
        _isFrooxWorkerInitialized = false;
    }

    private static void PreloadFrooxAssemblies()
    {
        Assembly.Load("FrooxEngine");
        Assembly.Load("ProtoFluxBindings");
        Assembly.Load("ProtoFlux.Nodes.FrooxEngine");
    }

    private static List<Type> GetAllFrooxTypes()
    {
        var list = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            list.AddRange(assembly.GetTypes());
        }

        return list;
    }

}
