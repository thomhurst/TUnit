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
    
    // Hook invokers removed - hooks now use direct context passing
    // Data source factories removed - now using inline delegates in TestDataSource objects
    
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
    
    // Hook invoker registration removed - hooks now use direct context passing
    // Data source factory registration removed - now using inline delegates
    
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
    
    // Hook invoker methods removed - hooks now use direct context passing
    // Data source factory methods removed - now using inline delegates
    
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