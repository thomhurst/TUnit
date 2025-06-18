using System.Reflection;
using TUnit.Core.Data;

namespace TUnit.Core;

/// <summary>
/// Represents a container for test data with improved error handling and unified data structures.
/// </summary>
internal static class TestDataContainer
{
    // Unified containers using the new improved architecture
    private static readonly ScopedContainer<string> GlobalContainer = new();
    private static readonly ScopedContainer<Type> ClassContainer = new();
    private static readonly ScopedContainer<Assembly> AssemblyContainer = new();
    private static readonly ScopedContainer<string> KeyContainer = new();

    // Note: Dependency tracking has been moved to the framework level

    /// <summary>
    /// Gets an instance for the specified class.
    /// </summary>
    /// <param name="testClass">The test class type.</param>
    /// <param name="type">The type of object to retrieve</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static object GetInstanceForClass(Type testClass, Type type, Func<object> func)
    {
        var scopedInstance = ClassContainer.GetOrCreate(testClass, type, func);
        return scopedInstance.Instance;
    }

    /// <summary>
    /// Gets an instance for the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="type">The type of object to retrieve</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static object GetInstanceForAssembly(Assembly assembly, Type type, Func<object> func)
    {
        var scopedInstance = AssemblyContainer.GetOrCreate(assembly, type, func);
        return scopedInstance.Instance;
    }

    /// <summary>
    /// Gets a global instance of the specified type.
    /// </summary>
    /// <param name="type">The type of object to retrieve</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static object GetGlobalInstance(Type type, Func<object> func)
    {
        var scopedInstance = GlobalContainer.GetOrCreate(typeof(object).FullName!, type, func);
        return scopedInstance.Instance;
    }

    /// <summary>
    /// Gets an instance for the specified key.
    /// </summary>
    /// <param name="type">The type of object to retrieve</param>
    /// <param name="key">The key.</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static object GetInstanceForKey(string key, Type type, Func<object> func)
    {
        var scopedInstance = KeyContainer.GetOrCreate(key, type, func);
        return scopedInstance.Instance;
    }
}
