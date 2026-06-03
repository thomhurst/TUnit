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
        return _assemblyContainer.GetOrCreate(assembly, type, func);
    }

    public static object? GetGlobalInstance(Type type, Func<Type, object> func)
    {
        return _globalContainer.GetOrCreate(typeof(object).FullName!, type, func);
    }

    public static object? GetInstanceForKey(string key, Type type, Func<Type, object> func)
    {
        return _keyContainer.GetOrCreate(key, type, func);
    }

    /// <summary>
    /// Clears all cached shared instances. Called at the end of a run session so that a
    /// subsequent run request in the same process (e.g. IDE server mode) creates fresh
    /// instances instead of reusing already-disposed ones.
    /// </summary>
    public static void Reset()
    {
        _globalContainer.Clear();
        _classContainer.Clear();
        _assemblyContainer.Clear();
        _keyContainer.Clear();
    }
}
