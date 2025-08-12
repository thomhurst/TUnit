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
        return _assemblyContainer.GetOrCreate(assembly, type, func);
    }

    public static object GetGlobalInstance(Type type, Func<object> func)
    {
        return _globalContainer.GetOrCreate(typeof(object).FullName!, type, func);
    }

    public static object GetInstanceForKey(string key, Type type, Func<object> func)
    {
        return _keyContainer.GetOrCreate(key, type, func);
    }

    /// <summary>
    /// Checks if the specified object instance is managed by any of the scoped containers.
    /// Objects managed by scoped containers should not be disposed by individual reference counting.
    /// </summary>
    /// <param name="instance">The instance to check</param>
    /// <returns>True if the object is managed by a scoped container; otherwise, false</returns>
    public static bool IsObjectManaged(object instance)
    {
        return _globalContainer.ContainsInstance(instance) ||
               _classContainer.ContainsInstance(instance) ||
               _assemblyContainer.ContainsInstance(instance) ||
               _keyContainer.ContainsInstance(instance);
    }

}
