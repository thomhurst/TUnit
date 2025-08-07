using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using System.Collections.Concurrent;
using System.Reflection;

namespace TUnit.Performance.Tests;

/// <summary>
/// Comprehensive benchmarks for all optimization areas
/// </summary>
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, iterationCount: 10)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
[Config(typeof(AntiVirusFriendlyConfig))]
public class OptimizationBenchmarks
{
    private class AntiVirusFriendlyConfig : ManualConfig
    {
        public AntiVirusFriendlyConfig()
        {
            WithOptions(ConfigOptions.DisableOptimizationsValidator);
        }
    }
    
    // Benchmark 1: Delegate Caching for MethodDataSource
    [Benchmark(Description = "MethodDataSource - Without Caching")]
    public void MethodDataSource_WithoutCaching()
    {
        var method = typeof(TestDataProvider).GetMethod(nameof(TestDataProvider.GetData))!;
        for (int i = 0; i < 1000; i++)
        {
            // Simulate reflection invocation without caching
            var result = method.Invoke(null, null);
        }
    }
    
    [Benchmark(Description = "MethodDataSource - With Delegate Caching")]
    public void MethodDataSource_WithCaching()
    {
        var cache = new ConcurrentDictionary<MethodInfo, Func<object?>>();
        var method = typeof(TestDataProvider).GetMethod(nameof(TestDataProvider.GetData))!;
        
        for (int i = 0; i < 1000; i++)
        {
            var func = cache.GetOrAdd(method, m =>
            {
                return () => m.Invoke(null, null);
            });
            var result = func();
        }
    }
    
    // Benchmark 2: EventReceiverRegistry Lock vs ConcurrentDictionary
    [Benchmark(Description = "EventRegistry - ReaderWriterLock")]
    public void EventRegistry_WithLock()
    {
        var rwLock = new ReaderWriterLockSlim();
        var registry = new Dictionary<Type, List<object>>();
        
        Parallel.For(0, 100, i =>
        {
            if (i % 10 == 0)
            {
                rwLock.EnterWriteLock();
                try
                {
                    registry[typeof(object)] = new List<object> { new object() };
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }
            else
            {
                rwLock.EnterReadLock();
                try
                {
                    _ = registry.TryGetValue(typeof(object), out _);
                }
                finally
                {
                    rwLock.ExitReadLock();
                }
            }
        });
        
        rwLock.Dispose();
    }
    
    [Benchmark(Description = "EventRegistry - ConcurrentDictionary")]
    public void EventRegistry_ConcurrentDictionary()
    {
        var registry = new ConcurrentDictionary<Type, object[]>();
        
        Parallel.For(0, 100, i =>
        {
            if (i % 10 == 0)
            {
                registry.AddOrUpdate(typeof(object),
                    _ => new[] { new object() },
                    (_, existing) => 
                    {
                        var newArray = new object[existing.Length + 1];
                        Array.Copy(existing, newArray, existing.Length);
                        newArray[existing.Length] = new object();
                        return newArray;
                    });
            }
            else
            {
                _ = registry.TryGetValue(typeof(object), out _);
            }
        });
    }
    
    // Benchmark 3: Reflection Caching
    private readonly ConcurrentDictionary<Type, MethodInfo[]> _methodCache = new();
    
    [Benchmark(Description = "Reflection - Without Caching")]
    public void Reflection_WithoutCaching()
    {
        var type = typeof(TestClass);
        for (int i = 0; i < 100; i++)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            _ = methods.Length;
        }
    }
    
    [Benchmark(Description = "Reflection - With Caching")]
    public void Reflection_WithCaching()
    {
        var type = typeof(TestClass);
        for (int i = 0; i < 100; i++)
        {
            var methods = _methodCache.GetOrAdd(type, t =>
                t.GetMethods(BindingFlags.Public | BindingFlags.Instance));
            _ = methods.Length;
        }
    }
    
    // Benchmark 4: Async Enumeration Optimization
    [Benchmark(Description = "AsyncEnum - Without ConfigureAwait")]
    public async Task AsyncEnum_WithoutConfigureAwait()
    {
        var count = 0;
        await foreach (var item in GetAsyncData())
        {
            count++;
            if (count >= 100) break;
        }
    }
    
    [Benchmark(Description = "AsyncEnum - With ConfigureAwait")]
    public async Task AsyncEnum_WithConfigureAwait()
    {
        var count = 0;
        await foreach (var item in GetAsyncData().ConfigureAwait(false))
        {
            count++;
            if (count >= 100) break;
        }
    }
    
    // Helper methods and classes
    private async IAsyncEnumerable<int> GetAsyncData()
    {
        for (int i = 0; i < 100; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
    
    private class TestClass
    {
        public void Method1() { }
        public void Method2() { }
        public void Method3() { }
    }
    
    private static class TestDataProvider
    {
        public static IEnumerable<int> GetData()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }
    }
}