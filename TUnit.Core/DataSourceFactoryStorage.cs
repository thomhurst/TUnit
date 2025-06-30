using System;
using System.Collections.Generic;
using System.Threading;

namespace TUnit.Core;

/// <summary>
/// Central storage for data source factories used in AOT scenarios
/// </summary>
public static class DataSourceFactoryStorage
{
    private static readonly Dictionary<string, Func<CancellationToken, IAsyncEnumerable<object?[]>>> _factories = new();
    private static readonly object _lock = new();
    
    /// <summary>
    /// Registers a data source factory
    /// </summary>
    public static void RegisterFactory(string key, Func<CancellationToken, IAsyncEnumerable<object?[]>> factory)
    {
        lock (_lock)
        {
            _factories[key] = factory;
        }
    }
    
    /// <summary>
    /// Gets a data source factory by key
    /// </summary>
    public static Func<CancellationToken, IAsyncEnumerable<object?[]>>? GetFactory(string key)
    {
        lock (_lock)
        {
            return _factories.TryGetValue(key, out var factory) ? factory : null;
        }
    }
    
    /// <summary>
    /// Clears all registered factories (useful for testing)
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _factories.Clear();
        }
    }
}