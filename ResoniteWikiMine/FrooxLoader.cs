using System.Reflection;
using FrooxEngine;

namespace ResoniteWikiMine;

public static class FrooxLoader
{
    private static bool _isFrooxWorkerInitialized;
    public static readonly List<Assembly> FrooxAssemblies = [];

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

        _isFrooxWorkerInitialized = true;
    }

    private static void PreloadFrooxAssemblies()
    {
        FrooxAssemblies.Add(Assembly.Load("FrooxEngine"));
        FrooxAssemblies.Add(Assembly.Load("ProtoFluxBindings"));
        FrooxAssemblies.Add(Assembly.Load("ProtoFlux.Nodes.FrooxEngine"));
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

    public static Type? FindFrooxType(string name)
    {
        if (!_isFrooxWorkerInitialized)
            throw new InvalidOperationException();

        foreach (var assembly in FrooxLoader.FrooxAssemblies)
        {
            if (assembly.GetType(name) is { } type)
                return type;
        }

        return null;
    }
}
