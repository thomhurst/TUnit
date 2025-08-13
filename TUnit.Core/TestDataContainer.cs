using System.Reflection;
using TUnit.Core.Data;

namespace TUnit.Core;

internal static class TestDataContainer
{
    private static readonly ScopedDictionary<string> _globalContainer = new();
    private static readonly ScopedDictionary<Type> _classContainer = new();
    private static readonly ScopedDictionary<Assembly> _assemblyContainer = new();
    private static readonly ScopedDictionary<string> _keyContainer = new();

    public static object? GetInstanceForClass(Type testClass, Type type, Func<Type, object> func)
    {
        return _classContainer.GetOrCreate(testClass, type, func);
    }

    public static object? GetInstanceForAssembly(Assembly assembly, Type type, Func<Type, object> func)
    {
        Console.WriteLine($"[TestDataContainer] GetInstanceForAssembly called for type {type.Name}, assembly {assembly.GetName().Name}");
        return _assemblyContainer.GetOrCreate(assembly, type, func);
    }

    public static object? GetGlobalInstance(Type type, Func<Type, object> func)
    {
        Console.WriteLine($"[TestDataContainer] GetGlobalInstance called for type {type.Name}");
        return _globalContainer.GetOrCreate(typeof(object).FullName!, type, func);
    }

    public static object? GetInstanceForKey(string key, Type type, Func<Type, object> func)
    {
        return _keyContainer.GetOrCreate(key, type, func);
    }
}
