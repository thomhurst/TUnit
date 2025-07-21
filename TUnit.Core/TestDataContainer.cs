using System.Reflection;
using TUnit.Core.Data;

namespace TUnit.Core;

internal static class TestDataContainer
{
    private static readonly ScopedContainer<string> GlobalContainer = new();
    private static readonly ScopedContainer<Type> ClassContainer = new();
    private static readonly ScopedContainer<Assembly> AssemblyContainer = new();
    private static readonly ScopedContainer<string> KeyContainer = new();

    public static object GetInstanceForClass(Type testClass, Type type, Func<object> func)
    {
        var scopedInstance = ClassContainer.GetOrCreate(testClass, type, func);
        return scopedInstance.Instance;
    }

    public static object GetInstanceForAssembly(Assembly assembly, Type type, Func<object> func)
    {
        var scopedInstance = AssemblyContainer.GetOrCreate(assembly, type, func);
        return scopedInstance.Instance;
    }

    public static object GetGlobalInstance(Type type, Func<object> func)
    {
        var scopedInstance = GlobalContainer.GetOrCreate(typeof(object).FullName!, type, func);
        return scopedInstance.Instance;
    }

    public static object GetInstanceForKey(string key, Type type, Func<object> func)
    {
        var scopedInstance = KeyContainer.GetOrCreate(key, type, func);
        return scopedInstance.Instance;
    }
}
