using System.Reflection;
using Elements.Core;
using FrooxEngine;

namespace ResoniteWikiMine;

public static class FrooxLoader
{
    private static bool _isFrooxWorkerInitialized;
    public static readonly List<Assembly> FrooxAssemblies = [];
    public static readonly List<AssemblyTypeRegistry> FrooxTypeRegistries = [];

    public static void InitializeFrooxWorker()
    {
        if (_isFrooxWorkerInitialized)
            return;

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Loading Froox Assemblies");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        PreloadFrooxAssemblies();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Loading Froox type registry");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        InitializeTypeRegistry();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Initializing workers");
        Console.ForegroundColor = ConsoleColor.DarkGray;

        WorkerInitializer.Initialize(GetAllFrooxTypes(), true);
        Console.ResetColor();

        _isFrooxWorkerInitialized = true;
    }

    private static void PreloadFrooxAssemblies()
    {
        FrooxAssemblies.Add(Assembly.Load("Elements.Core"));
        FrooxAssemblies.Add(Assembly.Load("Elements.Assets"));
        FrooxAssemblies.Add(Assembly.Load("FrooxEngine"));
        FrooxAssemblies.Add(Assembly.Load("ProtoFluxBindings"));
        FrooxAssemblies.Add(Assembly.Load("ProtoFlux.Nodes.FrooxEngine"));
    }

    private static void InitializeTypeRegistry()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var attr = assembly.GetCustomAttribute<DataModelAssemblyAttribute>();
            if (attr == null)
                continue;

            var registry = GlobalTypeRegistry.RegisterAssembly(assembly, attr.AssemblyType, assembly.GetTypes());
            FrooxTypeRegistries.Add(registry);
        }

        typeof(GlobalTypeRegistry).GetMethod("FinalizeTypes", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, []);
    }

    private static List<Type> GetAllFrooxTypes()
    {
        var list = new List<Type>();

        foreach (var assembly in FrooxAssemblies)
        {
            list.AddRange(assembly.GetTypes());
        }

        return list;
    }

    public static Type? FindFrooxType(string name)
    {
        if (!_isFrooxWorkerInitialized)
            throw new InvalidOperationException();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetType(name) is { } type)
                return type;
        }

        return null;
    }

    public static Type? GetType(string fullName)
    {
        foreach (var frooxAssembly in FrooxAssemblies)
        {
            if (frooxAssembly.GetType(fullName) is { } matched)
                return matched;
        }

        return null;
    }
}
