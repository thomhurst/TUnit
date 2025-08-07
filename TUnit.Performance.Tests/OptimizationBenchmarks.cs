using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;

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
    private readonly object _lock = new();
    private readonly Dictionary<Type, List<object>> _lockBasedRegistry = new();
    private readonly ConcurrentDictionary<Type, ImmutableArray<object>> _concurrentRegistry = new();
    
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
    }
    
    [Benchmark(Description = "EventRegistry - ConcurrentDictionary")]
    public void EventRegistry_ConcurrentDictionary()
    {
        var registry = new ConcurrentDictionary<Type, ImmutableArray<object>>();
        
        Parallel.For(0, 100, i =>
        {
            if (i % 10 == 0)
            {
                registry.AddOrUpdate(typeof(object),
                    _ => ImmutableArray.Create(new object()),
                    (_, existing) => existing.Add(new object()));
            }
            else
            {
                _ = registry.TryGetValue(typeof(object), out _);
            }
        });
    }
    
    // Benchmark 3: Sequential vs Parallel Test Building
    [Benchmark(Description = "TestBuilding - Sequential")]
    public async Task TestBuilding_Sequential()
    {
        var tests = Enumerable.Range(0, 100).Select(i => new TestMetadata { Id = i });
        var results = new List<ProcessedTest>();
        
        foreach (var test in tests)
        {
            // Simulate test building work
            await Task.Delay(1);
            results.Add(new ProcessedTest { Id = test.Id });
        }
    }
    
    [Benchmark(Description = "TestBuilding - Parallel")]
    public async Task TestBuilding_Parallel()
    {
        var tests = Enumerable.Range(0, 100).Select(i => new TestMetadata { Id = i });
        var results = new ConcurrentBag<ProcessedTest>();
        
        await Task.Run(() =>
        {
            Parallel.ForEach(tests, async test =>
            {
                // Simulate test building work
                await Task.Delay(1);
                results.Add(new ProcessedTest { Id = test.Id });
            });
        });
    }
    
    // Benchmark 4: Reflection Caching
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
    
    // Benchmark 5: Async Enumeration Optimization
    [Benchmark(Description = "AsyncEnum - Without ConfigureAwait")]
    public async Task AsyncEnum_WithoutConfigureAwait()
    {
        await foreach (var item in GetAsyncData())
        {
            await ProcessItem(item);
        }
    }
    
    [Benchmark(Description = "AsyncEnum - With ConfigureAwait")]
    public async Task AsyncEnum_WithConfigureAwait()
    {
        await foreach (var item in GetAsyncData().ConfigureAwait(false))
        {
            await ProcessItem(item).ConfigureAwait(false);
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
    
    private async Task ProcessItem(int item)
    {
        await Task.Delay(1);
    }
    
    private class TestMetadata
    {
        public int Id { get; set; }
    }
    
    private class ProcessedTest
    {
        public int Id { get; set; }
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

// Use ImmutableArray since we're using it in our optimizations
using ImmutableArray = System.Collections.Immutable.ImmutableArray<object>;