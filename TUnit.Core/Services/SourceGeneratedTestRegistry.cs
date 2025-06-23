using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// Default implementation of source-generated test registry.
/// Provides thread-safe storage for source-generated test factories and invokers.
/// Supports both strongly typed and weakly typed factories for maximum performance and compatibility.
/// </summary>
public class SourceGeneratedTestRegistry : ISourceGeneratedTestRegistry
{
    // Strongly typed factories (preferred for performance)
    private readonly ConcurrentDictionary<string, Delegate> _stronglyTypedClassFactories = new();
    private readonly ConcurrentDictionary<string, Delegate> _stronglyTypedMethodInvokers = new();
    
    // Method data source resolvers (AOT-safe MethodDataSource support)
    private readonly ConcurrentDictionary<string, Func<IReadOnlyList<object?[]>>> _methodDataResolvers = new();
    private readonly ConcurrentDictionary<string, Func<Task<IReadOnlyList<object?[]>>>> _asyncMethodDataResolvers = new();
    
    // Async data source resolvers (AOT-safe AsyncDataSourceGenerator support)
    private readonly ConcurrentDictionary<string, Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<Func<Task<object?[]?>>>>>> _asyncDataSourceResolvers = new();
    private readonly ConcurrentDictionary<string, Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<object?[]?>>>> _asyncDataExecutors = new();
    
    // Weakly typed factories (backward compatibility)
    private readonly ConcurrentDictionary<string, Func<object>> _classFactories = new();
    private readonly ConcurrentDictionary<string, Func<object?[], object>> _parameterizedClassFactories = new();
    private readonly ConcurrentDictionary<string, Func<object, object?[], Task<object?>>> _methodInvokers = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Action<object, object?>>> _propertySetters = new();
    private readonly ConcurrentDictionary<string, CompileTimeResolvedData> _resolvedData = new();

    /// <summary>
    /// Registers a strongly typed class factory for maximum performance.
    /// </summary>
    public void RegisterStronglyTypedClassFactory<T>(string testId, Func<T> factory) where T : class
    {
        _stronglyTypedClassFactories.TryAdd(testId, factory);
    }

    /// <summary>
    /// Registers a strongly typed parameterized class factory.
    /// </summary>
    public void RegisterStronglyTypedClassFactory<T>(string testId, Delegate factory) where T : class
    {
        _stronglyTypedClassFactories.TryAdd($"{testId}_parameterized", factory);
    }

    /// <summary>
    /// Registers a strongly typed method invoker for maximum performance.
    /// </summary>
    public void RegisterStronglyTypedMethodInvoker<T>(string testId, Delegate invoker) where T : class
    {
        _stronglyTypedMethodInvokers.TryAdd(testId, invoker);
    }

    /// <summary>
    /// Registers an AOT-safe method data resolver for MethodDataSource support.
    /// </summary>
    public void RegisterMethodDataResolver(string testId, Func<IReadOnlyList<object?[]>> resolver)
    {
        _methodDataResolvers.TryAdd(testId, resolver);
    }

    /// <summary>
    /// Registers an AOT-safe async method data resolver for MethodDataSource support.
    /// </summary>
    public void RegisterAsyncMethodDataResolver(string testId, Func<Task<IReadOnlyList<object?[]>>> resolver)
    {
        _asyncMethodDataResolvers.TryAdd(testId, resolver);
    }

    /// <summary>
    /// Registers an AOT-safe async data source resolver for AsyncDataSourceGenerator support.
    /// </summary>
    public void RegisterAsyncDataSourceResolver(string testId, Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<Func<Task<object?[]?>>>>> resolver)
    {
        _asyncDataSourceResolvers.TryAdd(testId, resolver);
    }

    /// <summary>
    /// Registers an AOT-safe async data executor for AsyncDataSourceGenerator support.
    /// </summary>
    public void RegisterAsyncDataExecutor(string testId, Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<object?[]?>>> executor)
    {
        _asyncDataExecutors.TryAdd(testId, executor);
    }

    /// <inheritdoc />
    public void RegisterClassFactory(string testId, Func<object> factory)
    {
        _classFactories.TryAdd(testId, factory);
    }

    /// <inheritdoc />
    public void RegisterClassFactory(string testId, Func<object?[], object> factory)
    {
        _parameterizedClassFactories.TryAdd(testId, factory);
    }

    /// <inheritdoc />
    public void RegisterMethodInvoker(string testId, Func<object, object?[], Task<object?>> invoker)
    {
        _methodInvokers.TryAdd(testId, invoker);
    }

    /// <inheritdoc />
    public void RegisterPropertySetter(string testId, string propertyName, Action<object, object?> setter)
    {
        var propertySetters = _propertySetters.GetOrAdd(testId, _ => new ConcurrentDictionary<string, Action<object, object?>>());
        propertySetters.TryAdd(propertyName, setter);
    }

    /// <inheritdoc />
    public void RegisterResolvedData(string testId, CompileTimeResolvedData resolvedData)
    {
        _resolvedData.TryAdd(testId, resolvedData);
    }

    /// <summary>
    /// Gets a strongly typed class factory for maximum performance.
    /// </summary>
    public T? GetStronglyTypedClassFactory<T>(string testId) where T : Delegate
    {
        if (_stronglyTypedClassFactories.TryGetValue(testId, out var factory))
        {
            return factory as T;
        }
        return null;
    }

    /// <summary>
    /// Gets a strongly typed method invoker for maximum performance.
    /// </summary>
    public T? GetStronglyTypedMethodInvoker<T>(string testId) where T : Delegate
    {
        if (_stronglyTypedMethodInvokers.TryGetValue(testId, out var invoker))
        {
            return invoker as T;
        }
        return null;
    }

    /// <summary>
    /// Gets an AOT-safe method data resolver.
    /// </summary>
    public Func<IReadOnlyList<object?[]>>? GetMethodDataResolver(string testId)
    {
        _methodDataResolvers.TryGetValue(testId, out var resolver);
        return resolver;
    }

    /// <summary>
    /// Gets an AOT-safe async method data resolver.
    /// </summary>
    public Func<Task<IReadOnlyList<object?[]>>>? GetAsyncMethodDataResolver(string testId)
    {
        _asyncMethodDataResolvers.TryGetValue(testId, out var resolver);
        return resolver;
    }

    /// <summary>
    /// Gets an AOT-safe async data source resolver.
    /// </summary>
    public Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<Func<Task<object?[]?>>>>>? GetAsyncDataSourceResolver(string testId)
    {
        _asyncDataSourceResolvers.TryGetValue(testId, out var resolver);
        return resolver;
    }

    /// <summary>
    /// Gets an AOT-safe async data executor.
    /// </summary>
    public Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<object?[]?>>>? GetAsyncDataExecutor(string testId)
    {
        _asyncDataExecutors.TryGetValue(testId, out var executor);
        return executor;
    }

    /// <summary>
    /// Checks if a method data resolver is available for the given test.
    /// </summary>
    public bool HasMethodDataResolver(string testId)
    {
        return _methodDataResolvers.ContainsKey(testId) || _asyncMethodDataResolvers.ContainsKey(testId);
    }

    /// <summary>
    /// Checks if an async data source resolver is available for the given test.
    /// </summary>
    public bool HasAsyncDataSourceResolver(string testId)
    {
        return _asyncDataSourceResolvers.ContainsKey(testId) || _asyncDataExecutors.ContainsKey(testId);
    }

    /// <summary>
    /// Checks if a strongly typed factory is available for the given test.
    /// </summary>
    public bool HasStronglyTypedFactory(string testId)
    {
        return _stronglyTypedClassFactories.ContainsKey(testId) && _stronglyTypedMethodInvokers.ContainsKey(testId);
    }

    /// <inheritdoc />
    public Func<object>? GetClassFactory(string testId)
    {
        _classFactories.TryGetValue(testId, out var factory);
        return factory;
    }

    /// <inheritdoc />
    public Func<object?[], object>? GetParameterizedClassFactory(string testId)
    {
        _parameterizedClassFactories.TryGetValue(testId, out var factory);
        return factory;
    }

    /// <inheritdoc />
    public Func<object, object?[], Task<object?>>? GetMethodInvoker(string testId)
    {
        _methodInvokers.TryGetValue(testId, out var invoker);
        return invoker;
    }

    /// <inheritdoc />
    public IDictionary<string, Action<object, object?>> GetPropertySetters(string testId)
    {
        if (_propertySetters.TryGetValue(testId, out var setters))
        {
            return setters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        return new Dictionary<string, Action<object, object?>>();
    }

    /// <inheritdoc />
    public CompileTimeResolvedData? GetResolvedData(string testId)
    {
        _resolvedData.TryGetValue(testId, out var data);
        return data;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetRegisteredTestIds()
    {
        var allIds = new HashSet<string>();
        allIds.UnionWith(_stronglyTypedClassFactories.Keys);
        allIds.UnionWith(_stronglyTypedMethodInvokers.Keys);
        allIds.UnionWith(_methodDataResolvers.Keys);
        allIds.UnionWith(_asyncMethodDataResolvers.Keys);
        allIds.UnionWith(_asyncDataSourceResolvers.Keys);
        allIds.UnionWith(_asyncDataExecutors.Keys);
        allIds.UnionWith(_classFactories.Keys);
        allIds.UnionWith(_parameterizedClassFactories.Keys);
        allIds.UnionWith(_methodInvokers.Keys);
        allIds.UnionWith(_propertySetters.Keys);
        allIds.UnionWith(_resolvedData.Keys);
        return allIds.ToList().AsReadOnly();
    }
}

/// <summary>
/// Static registry access point for source generators.
/// Provides a global registry that can be accessed from generated code.
/// Supports both strongly typed and weakly typed registration for maximum performance.
/// </summary>
public static class GlobalSourceGeneratedTestRegistry
{
    private static readonly Lazy<SourceGeneratedTestRegistry> _instance = 
        new(() => new SourceGeneratedTestRegistry());

    /// <summary>
    /// Gets the global source-generated test registry instance.
    /// </summary>
    public static SourceGeneratedTestRegistry Instance => _instance.Value;

    /// <summary>
    /// Registers a strongly typed class factory for maximum performance.
    /// This method is called from source-generated code.
    /// </summary>
    public static void RegisterStronglyTypedClassFactory<T>(string testId, Func<T> factory) where T : class
    {
        Instance.RegisterStronglyTypedClassFactory(testId, factory);
    }

    /// <summary>
    /// Registers a strongly typed parameterized class factory.
    /// This method is called from source-generated code.
    /// </summary>
    public static void RegisterStronglyTypedClassFactory<T>(string testId, Delegate factory) where T : class
    {
        Instance.RegisterStronglyTypedClassFactory<T>(testId, factory);
    }

    /// <summary>
    /// Registers a strongly typed method invoker for maximum performance.
    /// This method is called from source-generated code.
    /// </summary>
    public static void RegisterStronglyTypedMethodInvoker<T>(string testId, Delegate invoker) where T : class
    {
        Instance.RegisterStronglyTypedMethodInvoker<T>(testId, invoker);
    }

    /// <summary>
    /// Registers an AOT-safe method data resolver for MethodDataSource support.
    /// This method is called from source-generated code.
    /// </summary>
    public static void RegisterMethodDataResolver(string testId, Func<IReadOnlyList<object?[]>> resolver)
    {
        Instance.RegisterMethodDataResolver(testId, resolver);
    }

    /// <summary>
    /// Registers an AOT-safe async method data resolver for MethodDataSource support.
    /// This method is called from source-generated code.
    /// </summary>
    public static void RegisterAsyncMethodDataResolver(string testId, Func<Task<IReadOnlyList<object?[]>>> resolver)
    {
        Instance.RegisterAsyncMethodDataResolver(testId, resolver);
    }

    /// <summary>
    /// Registers an AOT-safe async data source resolver for AsyncDataSourceGenerator support.
    /// This method is called from source-generated code.
    /// </summary>
    public static void RegisterAsyncDataSourceResolver(string testId, Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<Func<Task<object?[]?>>>>> resolver)
    {
        Instance.RegisterAsyncDataSourceResolver(testId, resolver);
    }

    /// <summary>
    /// Registers an AOT-safe async data executor for AsyncDataSourceGenerator support.
    /// This method is called from source-generated code.
    /// </summary>
    public static void RegisterAsyncDataExecutor(string testId, Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<object?[]?>>> executor)
    {
        Instance.RegisterAsyncDataExecutor(testId, executor);
    }

    /// <summary>
    /// Registers a simple test class factory (backward compatibility).
    /// This method is called from source-generated code.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <param name="factory">Factory delegate for creating test instances</param>
    public static void RegisterClassFactory(string testId, Func<object> factory)
    {
        Instance.RegisterClassFactory(testId, factory);
    }

    /// <summary>
    /// Registers a parameterized test class factory.
    /// This method is called from source-generated code.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <param name="factory">Factory delegate for creating test instances with arguments</param>
    public static void RegisterParameterizedClassFactory(string testId, Func<object?[], object> factory)
    {
        Instance.RegisterClassFactory(testId, factory);
    }

    /// <summary>
    /// Registers a test method invoker.
    /// This method is called from source-generated code.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <param name="invoker">Invoker delegate for calling test methods</param>
    public static void RegisterMethodInvoker(string testId, Func<object, object?[], Task<object?>> invoker)
    {
        Instance.RegisterMethodInvoker(testId, invoker);
    }

    /// <summary>
    /// Registers a property setter for dependency injection.
    /// This method is called from source-generated code.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <param name="propertyName">Name of the property to set</param>
    /// <param name="setter">Setter delegate for the property</param>
    public static void RegisterPropertySetter(string testId, string propertyName, Action<object, object?> setter)
    {
        Instance.RegisterPropertySetter(testId, propertyName, setter);
    }

    /// <summary>
    /// Registers pre-resolved data for a test.
    /// This method is called from source-generated code.
    /// </summary>
    /// <param name="testId">Unique identifier for the test</param>
    /// <param name="resolvedData">The pre-resolved test data</param>
    public static void RegisterResolvedData(string testId, CompileTimeResolvedData resolvedData)
    {
        Instance.RegisterResolvedData(testId, resolvedData);
    }
}