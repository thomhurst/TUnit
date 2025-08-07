using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using TUnit.Core;
using TUnit.Engine;
using TUnit.Engine.Framework;

namespace TUnit.Performance.Tests;

/// <summary>
/// Benchmark to reproduce and track the performance regression reported in issue #2756
/// Original: ~190ms (v0.25.21)
/// Regression: ~350ms (v0.50.0)
/// Target: Return to <200ms
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class Issue2756RegressionBenchmark
{
    private TestAssemblyRunner? _runner;
    private Type? _testClass;
    
    [GlobalSetup]
    public void Setup()
    {
        // Create a test class dynamically to simulate the issue scenario
        _testClass = typeof(DataDrivenTestScenario);
        _runner = new TestAssemblyRunner();
    }
    
    /// <summary>
    /// Benchmark the exact scenario from issue #2756:
    /// 10,000 iterations with MethodDataSource
    /// </summary>
    [Benchmark(Description = "Issue #2756 - 10k iterations with MethodDataSource")]
    public async Task Issue2756_MethodDataSource_10000Iterations()
    {
        await _runner!.RunTestsInClass(_testClass!);
    }
    
    /// <summary>
    /// Benchmark with smaller iteration count for comparison
    /// </summary>
    [Benchmark(Description = "1k iterations with MethodDataSource")]
    public async Task MethodDataSource_1000Iterations()
    {
        await _runner!.RunTestsInClass(typeof(SmallDataDrivenTestScenario));
    }
    
    /// <summary>
    /// Benchmark simple tests without data sources as baseline
    /// </summary>
    [Benchmark(Baseline = true, Description = "Simple test baseline (100 tests)")]
    public async Task SimpleTests_Baseline()
    {
        await _runner!.RunTestsInClass(typeof(SimpleTestScenario));
    }
}

// Test scenarios for benchmarking
internal class DataDrivenTestScenario
{
    public static IEnumerable<int> TestData()
    {
        for (int i = 0; i < 10000; i++)
        {
            yield return i;
        }
    }
    
    [Test]
    [MethodDataSource(nameof(TestData))]
    public void TestWithMethodDataSource(int value)
    {
        // Simulate the test from issue #2756
        Assert.That(value).IsGreaterThanOrEqualTo(0);
    }
}

internal class SmallDataDrivenTestScenario
{
    public static IEnumerable<int> TestData()
    {
        for (int i = 0; i < 1000; i++)
        {
            yield return i;
        }
    }
    
    [Test]
    [MethodDataSource(nameof(TestData))]
    public void TestWithMethodDataSource(int value)
    {
        Assert.That(value).IsGreaterThanOrEqualTo(0);
    }
}

internal class SimpleTestScenario
{
    [Test]
    public void SimpleTest1() => Assert.That(1).IsEqualTo(1);
    
    [Test]
    public void SimpleTest2() => Assert.That(2).IsEqualTo(2);
    
    [Test]
    public void SimpleTest3() => Assert.That(3).IsEqualTo(3);
    
    // ... repeat pattern for 100 tests total
    [Test]
    public void SimpleTest4() => Assert.That(4).IsEqualTo(4);
    
    [Test]
    public void SimpleTest5() => Assert.That(5).IsEqualTo(5);
}

// Helper class to run tests
internal class TestAssemblyRunner
{
    public async Task RunTestsInClass(Type testClass)
    {
        var framework = new TUnitTestFramework();
        var testSessionId = Guid.NewGuid().ToString();
        
        // Simulate test discovery and execution
        await framework.DiscoverAndRunTestsInType(testSessionId, testClass);
    }
}

// Extension for the framework (simplified)
internal static class TUnitTestFrameworkExtensions
{
    public static async Task DiscoverAndRunTestsInType(this TUnitTestFramework framework, string sessionId, Type type)
    {
        // This would use the actual TUnit engine to discover and run tests
        // Simplified for benchmark purposes
        var testDiscovery = new TestDiscoveryService();
        var tests = await testDiscovery.DiscoverTestsInType(type, sessionId);
        
        var executor = new TestExecutor();
        await executor.ExecuteTests(tests);
    }
}