using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Linq.Expressions;
using System.Reflection;

namespace TUnit.Performance.Tests;

/// <summary>
/// Benchmark to reproduce and track the performance regression reported in issue #2756
/// Focuses on MethodDataSource reflection optimization
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class Issue2756RegressionBenchmark
{
    private MethodInfo? _testMethod;
    private Func<object?, object?[], object?>? _compiledDelegate;
    
    [GlobalSetup]
    public void Setup()
    {
        _testMethod = typeof(DataProvider).GetMethod(nameof(DataProvider.GetTestData))!;
        
        // Compile delegate using expression trees (simulating our optimization)
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var argumentsParam = Expression.Parameter(typeof(object[]), "arguments");
        
        var methodCall = Expression.Call(_testMethod);
        var lambda = Expression.Lambda<Func<object?, object?[], object?>>(
            Expression.Convert(methodCall, typeof(object)),
            instanceParam,
            argumentsParam);
        
        _compiledDelegate = lambda.Compile();
    }
    
    /// <summary>
    /// Benchmark reflection-based method invocation (old approach - slow)
    /// </summary>
    [Benchmark(Baseline = true, Description = "Reflection.Invoke (old/slow)")]
    public void ReflectionInvoke()
    {
        for (int i = 0; i < 10000; i++)
        {
            var result = _testMethod!.Invoke(null, null);
        }
    }
    
    /// <summary>
    /// Benchmark compiled delegate invocation (new approach - fast)
    /// </summary>
    [Benchmark(Description = "Compiled Delegate (new/fast)")]
    public void CompiledDelegate()
    {
        for (int i = 0; i < 10000; i++)
        {
            var result = _compiledDelegate!(null, null!);
        }
    }
    
    /// <summary>
    /// Direct method call for reference
    /// </summary>
    [Benchmark(Description = "Direct Call (reference)")]
    public void DirectCall()
    {
        for (int i = 0; i < 10000; i++)
        {
            var result = DataProvider.GetTestData();
        }
    }
}

// Test data provider
internal static class DataProvider
{
    public static IEnumerable<int> GetTestData()
    {
        // Return a simple enumerable to simulate MethodDataSource
        return Enumerable.Range(0, 10);
    }
}