using System.Reflection;
using TUnit.Core.Data;

namespace TUnit.Core;

internal static class TestDataContainer
{
    private static readonly ScopedContainer<string> _globalContainer = new();
    private static readonly ScopedContainer<Type> _classContainer = new();
    private static readonly ScopedContainer<Assembly> _assemblyContainer = new();
    private static readonly ScopedContainer<string> _keyContainer = new();

    public static object GetInstanceForClass(Type testClass, Type type, Func<object> func)
    {
        return _classContainer.GetOrCreate(testClass, type, func);
    }

    public static object GetInstanceForAssembly(Assembly assembly, Type type, Func<object> func)
    {
        Console.WriteLine($"[TestDataContainer] GetInstanceForAssembly called for type {type.Name}, assembly {assembly.GetName().Name}");
        return _assemblyContainer.GetOrCreate(assembly, type, func);
    }

    public static object GetGlobalInstance(Type type, Func<object> func)
    {
        Console.WriteLine($"[TestDataContainer] GetGlobalInstance called for type {type.Name}");
        return _globalContainer.GetOrCreate(typeof(object).FullName!, type, func);
    }

    public static object GetInstanceForKey(string key, Type type, Func<object> func)
    {
        return _keyContainer.GetOrCreate(key, type, func);
    }

    public static bool RemoveInstanceForClass(Type testClass, Type type, object instance)
    {
        return _classContainer.Remove(testClass, type, instance);
    }

    public static bool RemoveInstanceForAssembly(Assembly assembly, Type type, object instance)
    {
        return _assemblyContainer.Remove(assembly, type, instance);
    }

    public static bool RemoveGlobalInstance(Type type, object instance)
    {
        return _globalContainer.Remove(typeof(object).FullName!, type, instance);
    }

    public static bool RemoveInstanceForKey(string key, Type type, object instance)
    {
        return _keyContainer.Remove(key, type, instance);
    }
}
