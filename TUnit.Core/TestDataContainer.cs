using System.Reflection;
using System.Threading.Tasks;
using TUnit.Core.Data;
using TUnit.Core.Tracking;

namespace TUnit.Core;

internal static class TestDataContainer
{
    private static readonly ScopedDictionary<string> _globalContainer = new();
    private static readonly ScopedDictionary<Type> _classContainer = new();
    private static readonly ScopedDictionary<Assembly> _assemblyContainer = new();
    private static readonly ScopedDictionary<string> _keyContainer = new();

    public static object? GetInstanceForClass(Type testClass, Type type, Func<Type, object> func)
    {
        var result = _classContainer.GetOrCreate(testClass, type, func);
        Console.WriteLine($"[TestDataContainer] GetInstanceForClass({testClass.Name}, {type.Name}) = {result?.GetHashCode()}");
        return result;
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

    public static void ClearClassScope(Type testClass)
    {
        Console.WriteLine($"[TestDataContainer] Clearing class scope for {testClass.Name}");
        
        // Get all cached objects for this class and dispose them
        var cachedObjects = _classContainer.GetScopeValues(testClass);
        foreach (var cachedObject in cachedObjects)
        {
            if (cachedObject != null)
            {
                if (cachedObject is IDisposable disposable)
                {
                    Console.WriteLine($"[TestDataContainer] Disposing shared object {cachedObject.GetType().Name} (hash: {cachedObject.GetHashCode()}) for class {testClass.Name}");
                    ObjectTracker.UnmarkAsShared(cachedObject);
                    disposable.Dispose();
                }
                else if (cachedObject is IAsyncDisposable asyncDisposable)
                {
                    Console.WriteLine($"[TestDataContainer] Async disposing shared object {cachedObject.GetType().Name} (hash: {cachedObject.GetHashCode()}) for class {testClass.Name}");
                    ObjectTracker.UnmarkAsShared(cachedObject);
                    _ = Task.Run(async () => await asyncDisposable.DisposeAsync());
                }
            }
        }
        
        _classContainer.ClearScope(testClass);
    }

    public static void ClearAssemblyScope(Assembly assembly)
    {
        var cachedObjects = _assemblyContainer.GetScopeValues(assembly);
        foreach (var cachedObject in cachedObjects)
        {
            if (cachedObject != null)
            {
                if (cachedObject is IDisposable disposable)
                {
                    ObjectTracker.UnmarkAsShared(cachedObject);
                    disposable.Dispose();
                }
                else if (cachedObject is IAsyncDisposable asyncDisposable)
                {
                    ObjectTracker.UnmarkAsShared(cachedObject);
                    _ = Task.Run(async () => await asyncDisposable.DisposeAsync());
                }
            }
        }
        
        _assemblyContainer.ClearScope(assembly);
    }

    public static void ClearGlobalScope()
    {
        var cachedObjects = _globalContainer.GetScopeValues(typeof(object).FullName!);
        foreach (var cachedObject in cachedObjects)
        {
            if (cachedObject != null)
            {
                if (cachedObject is IDisposable disposable)
                {
                    ObjectTracker.UnmarkAsShared(cachedObject);
                    disposable.Dispose();
                }
                else if (cachedObject is IAsyncDisposable asyncDisposable)
                {
                    ObjectTracker.UnmarkAsShared(cachedObject);
                    _ = Task.Run(async () => await asyncDisposable.DisposeAsync());
                }
            }
        }
        
        _globalContainer.ClearScope(typeof(object).FullName!);
    }

    public static void ClearKeyScope(string key)
    {
        var cachedObjects = _keyContainer.GetScopeValues(key);
        foreach (var cachedObject in cachedObjects)
        {
            if (cachedObject != null)
            {
                if (cachedObject is IDisposable disposable)
                {
                    ObjectTracker.UnmarkAsShared(cachedObject);
                    disposable.Dispose();
                }
                else if (cachedObject is IAsyncDisposable asyncDisposable)
                {
                    ObjectTracker.UnmarkAsShared(cachedObject);
                    _ = Task.Run(async () => await asyncDisposable.DisposeAsync());
                }
            }
        }
        
        _keyContainer.ClearScope(key);
    }
}
