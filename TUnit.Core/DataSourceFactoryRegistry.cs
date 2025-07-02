using System;
using System.Collections.Generic;
using System.Threading;

namespace TUnit.Core;

/// <summary>
/// Registry for data source factories
/// </summary>
public static class DataSourceFactoryRegistry
{
    public static void Register(string key, Func<CancellationToken, IEnumerable<object?[]>> factory)
    {
        // Convert sync factory to async for storage
        DataSourceFactoryStorage.RegisterFactory(key, ct => ConvertToAsyncEnumerable(factory(ct), ct));
    }

    public static Func<CancellationToken, IEnumerable<object?[]>>? GetFactory(string key)
    {
        // This is for backward compatibility with tests
        var asyncFactory = DataSourceFactoryStorage.GetFactory(key);
        if (asyncFactory == null) return null;
        
        return ct =>
        {
            var asyncEnum = asyncFactory(ct);
            var result = new List<object?[]>();
            var enumerator = asyncEnum.GetAsyncEnumerator(ct);
            try
            {
                while (enumerator.MoveNextAsync().AsTask().Result)
                {
                    result.Add(enumerator.Current);
                }
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().Wait();
            }
            return result;
        };
    }
    
    private static async IAsyncEnumerable<object?[]> ConvertToAsyncEnumerable(
        IEnumerable<object?[]> data, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await Task.Yield(); // Ensure async behavior
        foreach (var item in data)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
        }
    }
}