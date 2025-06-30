using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Collections.Generic;
using System.Linq;
using TUnit.Core;
using TUnit.Engine;

namespace TUnit.Performance.Tests;

[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.NativeAot80)]
[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class TestDiscoveryBenchmarks
{
    private List<TestMetadata>? _testMetadata;

    [GlobalSetup]
    public void Setup()
    {
        // Pre-generate test metadata for benchmarking
        _testMetadata = new List<TestMetadata>();
        
        // Add various test metadata patterns
        for (int i = 0; i < 1000; i++)
        {
            _testMetadata.Add(CreateBasicTestMetadata($"TestMethod_{i}"));
            _testMetadata.Add(CreateParameterizedTestMetadata($"ParameterizedTest_{i}"));
            _testMetadata.Add(CreateAsyncTestMetadata($"AsyncTest_{i}"));
        }
    }

    [Benchmark(Baseline = true)]
    public void DiscoverTests_ReflectionMode()
    {
        // Simulate reflection-based discovery
        var tests = SimulateReflectionDiscovery();
        _ = tests.Count();
    }

    [Benchmark]
    public void DiscoverTests_DelegateMode()
    {
        // Use pre-compiled delegate discovery
        var tests = SimulateDelegateDiscovery();
        _ = tests.Count();
    }

    [Benchmark]
    public void DiscoverTests_WithFiltering()
    {
        // Test discovery with filtering
        var tests = SimulateDelegateDiscovery()
            .Where(t => t.TestName.Contains("Test_5"));
        _ = tests.Count();
    }

    [Benchmark]
    public void DiscoverTests_WithCategories()
    {
        // Test discovery with category filtering
        var tests = SimulateDelegateDiscovery()
            .Where(t => t.Categories?.Contains("Performance") ?? false);
        _ = tests.Count();
    }

    private IEnumerable<TestMetadata> SimulateReflectionDiscovery()
    {
        // Simulate the cost of reflection-based discovery
        foreach (var metadata in _testMetadata!)
        {
            // Simulate reflection overhead
            var type = metadata.GetType();
            var properties = type.GetProperties();
            var methods = type.GetMethods();
            
            yield return metadata;
        }
    }

    private IEnumerable<TestMetadata> SimulateDelegateDiscovery()
    {
        // Direct enumeration - no reflection overhead
        return _testMetadata!;
    }

    private TestMetadata CreateBasicTestMetadata(string name)
    {
        return new TestMetadata
        {
            TestId = $"BenchmarkTestClass.{name}",
            TestName = name,
            TestClassType = typeof(BenchmarkTestClass),
            TestMethodName = name,
            FilePath = "/test/file.cs",
            LineNumber = 10,
            Categories = new[] { "Unit", "Performance" }
        };
    }

    private TestMetadata CreateParameterizedTestMetadata(string name)
    {
        return new TestMetadata
        {
            TestId = $"BenchmarkTestClass.{name}",
            TestName = name,
            TestClassType = typeof(BenchmarkTestClass),
            TestMethodName = name,
            FilePath = "/test/file.cs",
            LineNumber = 20,
            Categories = new[] { "Integration" },
            ParameterCount = 3
        };
    }

    private TestMetadata CreateAsyncTestMetadata(string name)
    {
        return new TestMetadata
        {
            TestId = $"BenchmarkTestClass.{name}",
            TestName = name,
            TestClassType = typeof(BenchmarkTestClass),
            TestMethodName = name,
            FilePath = "/test/file.cs",
            LineNumber = 30,
            Categories = new[] { "Async" }
        };
    }

    // Dummy test class for metadata
    private class BenchmarkTestClass { }
}