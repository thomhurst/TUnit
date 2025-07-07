using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// Registry for test execution data.
/// Provides a simplified, single-responsibility storage for all test execution delegates and data.
/// </summary>
public class TestExecutionRegistry : ISourceGeneratedTestRegistry
{
    private readonly ConcurrentDictionary<string, TestExecutionData> _testData = new();

    /// <summary>
    /// Gets the singleton instance of the registry
    /// </summary>
    public static TestExecutionRegistry Instance { get; } = new();

    /// <summary>
    /// Registers or updates test execution data
    /// </summary>
    public void RegisterTest(string testId, TestExecutionData data)
    {
        _testData.AddOrUpdate(testId, data, (_, _) => data);
    }

    /// <summary>
    /// Gets test execution data for a specific test
    /// </summary>
    public TestExecutionData? GetTestData(string testId)
    {
        return _testData.TryGetValue(testId, out var data) ? data : null;
    }

    /// <summary>
    /// Gets or creates test execution data for a specific test
    /// </summary>
    public TestExecutionData GetOrCreateTestData(string testId)
    {
        return _testData.GetOrAdd(testId, _ => new TestExecutionData());
    }

    // ISourceGeneratedTestRegistry implementation for backward compatibility
    public void RegisterClassFactory(string testId, Func<object> factory)
    {
        var data = GetOrCreateTestData(testId);
        data.ClassFactory = factory;
    }

    public void RegisterClassFactory(string testId, Func<object?[], object> factory)
    {
        var data = GetOrCreateTestData(testId);
        data.ClassFactory = factory;
    }

    public void RegisterMethodInvoker(string testId, Func<object, object?[], Task<object?>> invoker)
    {
        var data = GetOrCreateTestData(testId);
        data.MethodInvoker = invoker;
    }

    public void RegisterPropertySetter(string testId, string propertyName, Action<object, object?> setter)
    {
        var data = GetOrCreateTestData(testId);
        data.PropertySetters.TryAdd(propertyName, setter);
    }


    public Func<object>? GetClassFactory(string testId)
    {
        var data = GetTestData(testId);
        return data?.ClassFactory as Func<object>;
    }

    public Func<object?[], object>? GetParameterizedClassFactory(string testId)
    {
        var data = GetTestData(testId);
        return data?.ClassFactory as Func<object?[], object>;
    }

    public Func<object, object?[], Task<object?>>? GetMethodInvoker(string testId)
    {
        var data = GetTestData(testId);
        return data?.MethodInvoker as Func<object, object?[], Task<object?>>;
    }

    public IDictionary<string, Action<object, object?>> GetPropertySetters(string testId)
    {
        var data = GetTestData(testId);
        return data?.PropertySetters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) 
            ?? new Dictionary<string, Action<object, object?>>();
    }


    public IReadOnlyCollection<string> GetRegisteredTestIds()
    {
        return _testData.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Clears all registered test data (useful for testing)
    /// </summary>
    public void Clear()
    {
        _testData.Clear();
    }

    // Additional strongly-typed registration methods
    public void RegisterStronglyTypedClassFactory<T>(string testId, Func<T> factory) where T : class
    {
        var data = GetOrCreateTestData(testId);
        data.ClassFactory = factory;
    }

    public void RegisterStronglyTypedClassFactory<T>(string testId, Delegate factory) where T : class
    {
        var data = GetOrCreateTestData(testId);
        data.ClassFactory = factory;
    }

    public void RegisterStronglyTypedMethodInvoker<T>(string testId, Delegate invoker) where T : class
    {
        var data = GetOrCreateTestData(testId);
        data.MethodInvoker = invoker;
    }

    public void RegisterMethodDataResolver(string testId, Func<IReadOnlyList<object?[]>> resolver)
    {
        var data = GetOrCreateTestData(testId);
        data.MethodDataResolver = resolver;
    }

    public void RegisterAsyncMethodDataResolver(string testId, Func<Task<IReadOnlyList<object?[]>>> resolver)
    {
        var data = GetOrCreateTestData(testId);
        data.AsyncMethodDataResolver = resolver;
    }

    public void RegisterAsyncDataSourceResolver(string testId, 
        Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<Func<Task<object?[]?>>>>> resolver)
    {
        var data = GetOrCreateTestData(testId);
        data.AsyncDataSourceResolver = resolver;
    }

    public void RegisterAsyncDataExecutor(string testId, 
        Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<object?[]?>>> executor)
    {
        var data = GetOrCreateTestData(testId);
        data.AsyncDataExecutor = executor;
    }

    // Strongly-typed getters
    public T? GetStronglyTypedClassFactory<T>(string testId) where T : Delegate
    {
        var data = GetTestData(testId);
        return data?.ClassFactory as T;
    }

    public T? GetStronglyTypedMethodInvoker<T>(string testId) where T : Delegate
    {
        var data = GetTestData(testId);
        return data?.MethodInvoker as T;
    }

    public Func<IReadOnlyList<object?[]>>? GetMethodDataResolver(string testId)
    {
        return GetTestData(testId)?.MethodDataResolver;
    }

    public Func<Task<IReadOnlyList<object?[]>>>? GetAsyncMethodDataResolver(string testId)
    {
        return GetTestData(testId)?.AsyncMethodDataResolver;
    }

    public Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<Func<Task<object?[]?>>>>>? 
        GetAsyncDataSourceResolver(string testId)
    {
        return GetTestData(testId)?.AsyncDataSourceResolver;
    }

    public Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<object?[]?>>>? 
        GetAsyncDataExecutor(string testId)
    {
        return GetTestData(testId)?.AsyncDataExecutor;
    }

    public bool HasMethodDataResolver(string testId)
    {
        return GetTestData(testId)?.HasMethodDataResolver ?? false;
    }

    public bool HasAsyncDataSourceResolver(string testId)
    {
        return GetTestData(testId)?.HasAsyncDataSource ?? false;
    }

    public bool HasStronglyTypedFactory(string testId)
    {
        return GetTestData(testId)?.HasStronglyTypedDelegates ?? false;
    }
}