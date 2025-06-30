using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TUnit.Core;

/// <summary>
/// Centralized storage for all source-generated test delegates.
/// This class is populated by source generators and provides AOT-safe access to test execution logic.
/// </summary>
public static class TestDelegateStorage
{
    /// <summary>
    /// Test method invokers indexed by fully qualified test name
    /// </summary>
    private static readonly Dictionary<string, Func<object, object?[], Task>> TestInvokers = new();
    
    /// <summary>
    /// Instance factories indexed by fully qualified class name
    /// </summary>
    private static readonly Dictionary<string, Func<object?[], object>> InstanceFactories = new();
    
    /// <summary>
    /// Property setters indexed by class.property name
    /// </summary>
    private static readonly Dictionary<string, Action<object, object?>> PropertySetters = new();
    
    /// <summary>
    /// Hook invokers indexed by class.method name
    /// </summary>
    private static readonly Dictionary<string, Func<object?, HookContext, Task>> HookInvokers = new();
    
    /// <summary>
    /// Data source factories indexed by unique identifier
    /// </summary>
    private static readonly Dictionary<string, Func<IEnumerable<object?[]>>> DataSourceFactories = new();
    
    /// <summary>
    /// Strongly-typed delegates indexed by method signature
    /// </summary>
    private static readonly Dictionary<string, Delegate> StronglyTypedDelegates = new();
    
    /// <summary>
    /// Type information for strongly-typed delegates
    /// </summary>
    private static readonly Dictionary<string, Type[]> DelegateParameterTypes = new();
    
    /// <summary>
    /// Bulk property setters indexed by class name
    /// </summary>
    private static readonly Dictionary<string, Action<object, IServiceProvider>> BulkPropertySetters = new();
    
    /// <summary>
    /// Register a test invoker
    /// </summary>
    public static void RegisterTestInvoker(string key, Func<object, object?[], Task> invoker)
    {
        TestInvokers[key] = invoker;
    }
    
    /// <summary>
    /// Register an instance factory
    /// </summary>
    public static void RegisterInstanceFactory(string key, Func<object?[], object> factory)
    {
        InstanceFactories[key] = factory;
    }
    
    /// <summary>
    /// Register a property setter
    /// </summary>
    public static void RegisterPropertySetter(string key, Action<object, object?> setter)
    {
        PropertySetters[key] = setter;
    }
    
    /// <summary>
    /// Register a hook invoker
    /// </summary>
    public static void RegisterHookInvoker(string key, Func<object?, HookContext, Task> invoker)
    {
        HookInvokers[key] = invoker;
    }
    
    /// <summary>
    /// Register a data source factory
    /// </summary>
    public static void RegisterDataSourceFactory(string key, Func<IEnumerable<object?[]>> factory)
    {
        DataSourceFactories[key] = factory;
    }
    
    /// <summary>
    /// Get a test invoker by key
    /// </summary>
    public static Func<object, object?[], Task>? GetTestInvoker(string key)
    {
        return TestInvokers.TryGetValue(key, out var invoker) ? invoker : null;
    }
    
    /// <summary>
    /// Get an instance factory by key
    /// </summary>
    public static Func<object?[], object>? GetInstanceFactory(string key)
    {
        return InstanceFactories.TryGetValue(key, out var factory) ? factory : null;
    }
    
    /// <summary>
    /// Get a property setter by key
    /// </summary>
    public static Action<object, object?>? GetPropertySetter(string key)
    {
        return PropertySetters.TryGetValue(key, out var setter) ? setter : null;
    }
    
    /// <summary>
    /// Get a hook invoker by key
    /// </summary>
    public static Func<object?, HookContext, Task>? GetHookInvoker(string key)
    {
        return HookInvokers.TryGetValue(key, out var invoker) ? invoker : null;
    }
    
    /// <summary>
    /// Get a data source factory by key
    /// </summary>
    public static Func<IEnumerable<object?[]>>? GetDataSourceFactory(string key)
    {
        return DataSourceFactories.TryGetValue(key, out var factory) ? factory : null;
    }
    
    /// <summary>
    /// Get an async data source factory by key (delegates to DataSourceFactoryStorage)
    /// </summary>
    public static Func<CancellationToken, IAsyncEnumerable<object?[]>>? GetAsyncDataSourceFactory(string key)
    {
        return DataSourceFactoryStorage.GetFactory(key);
    }
    
    /// <summary>
    /// Register a strongly-typed delegate
    /// </summary>
    public static void RegisterStronglyTypedDelegate(string key, Type[] parameterTypes, Delegate @delegate)
    {
        StronglyTypedDelegates[key] = @delegate;
        DelegateParameterTypes[key] = parameterTypes;
    }
    
    /// <summary>
    /// Get a strongly-typed delegate by key
    /// </summary>
    public static T? GetStronglyTypedDelegate<T>(string key) where T : Delegate
    {
        return StronglyTypedDelegates.TryGetValue(key, out var @delegate) ? @delegate as T : null;
    }
    
    /// <summary>
    /// Get a strongly-typed delegate by key (untyped)
    /// </summary>
    public static Delegate? GetStronglyTypedDelegate(string key)
    {
        return StronglyTypedDelegates.TryGetValue(key, out var @delegate) ? @delegate : null;
    }
    
    /// <summary>
    /// Get parameter types for a strongly-typed delegate
    /// </summary>
    public static Type[]? GetDelegateParameterTypes(string key)
    {
        return DelegateParameterTypes.TryGetValue(key, out var types) ? types : null;
    }
    
    /// <summary>
    /// Check if a strongly-typed delegate exists
    /// </summary>
    public static bool HasStronglyTypedDelegate(string key)
    {
        return StronglyTypedDelegates.ContainsKey(key);
    }
    
    /// <summary>
    /// Register a bulk property setter
    /// </summary>
    public static void RegisterBulkPropertySetter(string className, Action<object, IServiceProvider> setter)
    {
        BulkPropertySetters[className] = setter;
    }
    
    /// <summary>
    /// Get a bulk property setter by class name
    /// </summary>
    public static Action<object, IServiceProvider>? GetBulkPropertySetter(string className)
    {
        return BulkPropertySetters.TryGetValue(className, out var setter) ? setter : null;
    }
}