using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace TUnit.Performance.Tests;

/// <summary>
/// Performance tests for the TestBuilder architecture to ensure optimizations are working.
/// </summary>
public class TestBuilderPerformanceTests
{
    private const int WarmupIterations = 100;
    private const int TestIterations = 1000;
    
    [Test]
    public async Task TestBuilder_BasicVsOptimized_PerformanceComparison()
    {
        var metadata = CreateSampleTestMetadata();
        
        // Warmup both builders
        var basicBuilder = new TestBuilder();
        var optimizedBuilder = new TestBuilderOptimized();
        
        for (int i = 0; i < WarmupIterations; i++)
        {
            await basicBuilder.BuildTestsAsync(metadata);
            await optimizedBuilder.BuildTestsAsync(metadata);
        }
        
        // Measure basic builder
        var basicStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < TestIterations; i++)
        {
            await basicBuilder.BuildTestsAsync(metadata);
        }
        basicStopwatch.Stop();
        
        // Measure optimized builder
        var optimizedStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < TestIterations; i++)
        {
            await optimizedBuilder.BuildTestsAsync(metadata);
        }
        optimizedStopwatch.Stop();
        
        // Calculate improvement
        var improvement = (double)basicStopwatch.ElapsedMilliseconds / optimizedStopwatch.ElapsedMilliseconds;
        
        Console.WriteLine($"Basic: {basicStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Optimized: {optimizedStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Improvement: {improvement:F2}x faster");
        
        // Optimized should be at least 2x faster due to expression compilation and caching
        await Assert.That(improvement).IsGreaterThan(2.0);
    }
    
    [Test]
    public async Task TestBuilder_CachingEffectiveness()
    {
        var metadata = CreateSampleTestMetadata();
        var optimizedBuilder = new TestBuilderOptimized();
        
        // First run - cold cache
        var coldStopwatch = Stopwatch.StartNew();
        await optimizedBuilder.BuildTestsAsync(metadata);
        coldStopwatch.Stop();
        
        // Subsequent runs - warm cache
        var warmTimes = new long[10];
        for (int i = 0; i < 10; i++)
        {
            var warmStopwatch = Stopwatch.StartNew();
            await optimizedBuilder.BuildTestsAsync(metadata);
            warmStopwatch.Stop();
            warmTimes[i] = warmStopwatch.ElapsedTicks;
        }
        
        var avgWarmTime = warmTimes.Average();
        var cacheImprovement = (double)coldStopwatch.ElapsedTicks / avgWarmTime;
        
        Console.WriteLine($"Cold cache: {coldStopwatch.ElapsedTicks} ticks");
        Console.WriteLine($"Warm cache avg: {avgWarmTime:F0} ticks");
        Console.WriteLine($"Cache improvement: {cacheImprovement:F2}x faster");
        
        // Warm cache should be significantly faster
        await Assert.That(cacheImprovement).IsGreaterThan(5.0);
    }
    
    [Test]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public async Task TestBuilder_ScalesLinearly(int dataSourceSize)
    {
        var metadata = CreateTestMetadataWithDataSize(dataSourceSize);
        var optimizedBuilder = new TestBuilderOptimized();
        
        // Warmup
        await optimizedBuilder.BuildTestsAsync(metadata);
        
        // Measure
        var stopwatch = Stopwatch.StartNew();
        var tests = await optimizedBuilder.BuildTestsAsync(metadata);
        stopwatch.Stop();
        
        var testCount = tests.Count();
        var timePerTest = (double)stopwatch.ElapsedMilliseconds / testCount;
        
        Console.WriteLine($"Data size: {dataSourceSize}, Tests: {testCount}, Time: {stopwatch.ElapsedMilliseconds}ms, Per test: {timePerTest:F3}ms");
        
        // Time per test should remain relatively constant regardless of data size
        await Assert.That(timePerTest).IsLessThan(1.0); // Less than 1ms per test
    }
    
    [Test]
    public async Task TestBuilder_ParallelDataSourceProcessing()
    {
        var metadata = CreateTestMetadataWithMultipleDataSources();
        var optimizedBuilder = new TestBuilderOptimized();
        
        var stopwatch = Stopwatch.StartNew();
        var tests = await optimizedBuilder.BuildTestsAsync(metadata);
        stopwatch.Stop();
        
        // With parallel processing, this should be fast even with multiple data sources
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(100);
        await Assert.That(tests.Count()).IsGreaterThan(0);
    }
    
    [Test]
    public async Task TestBuilder_ExpressionCompilation_Performance()
    {
        var testType = typeof(PerformanceTestClass);
        var optimizedBuilder = new TestBuilderOptimized();
        
        // Create metadata with various method signatures
        var metadata = new TestMetadata
        {
            TestIdTemplate = "PerfTest_{TestIndex}",
            TestClassType = testType,
            TestMethod = testType.GetMethod("TestMethod")!,
            MethodMetadata = CreateMethodMetadata(testType),
            TestFilePath = "PerfTest.cs",
            TestLineNumber = 1,
            TestClassFactory = args => new PerformanceTestClass(),
            ClassDataSources = Array.Empty<IDataSourceProvider>(),
            MethodDataSources = new IDataSourceProvider[]
            {
                new InlineDataSourceProvider(1, "test", true),
                new InlineDataSourceProvider(2, "test2", false),
                new InlineDataSourceProvider(3, "test3", true)
            },
            PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
            DisplayNameTemplate = "PerfTest",
            RepeatCount = 1,
            IsAsync = false,
            IsSkipped = false,
            Attributes = Array.Empty<Attribute>(),
            Timeout = null
        };
        
        // Measure compilation + execution
        var stopwatch = Stopwatch.StartNew();
        var tests = await optimizedBuilder.BuildTestsAsync(metadata);
        stopwatch.Stop();
        
        await Assert.That(tests.Count()).IsEqualTo(3);
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(50);
    }
    
    private static TestMetadata CreateSampleTestMetadata()
    {
        var testType = typeof(SampleTestClass);
        return new TestMetadata
        {
            TestIdTemplate = "Test_{TestIndex}",
            TestClassType = testType,
            TestMethod = testType.GetMethod("TestMethod")!,
            MethodMetadata = CreateMethodMetadata(testType),
            TestFilePath = "Test.cs",
            TestLineNumber = 1,
            TestClassFactory = args => new SampleTestClass(),
            ClassDataSources = Array.Empty<IDataSourceProvider>(),
            MethodDataSources = new IDataSourceProvider[]
            {
                new InlineDataSourceProvider(1, 2, 3),
                new InlineDataSourceProvider(4, 5, 6)
            },
            PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
            DisplayNameTemplate = "Test",
            RepeatCount = 1,
            IsAsync = false,
            IsSkipped = false,
            Attributes = Array.Empty<Attribute>(),
            Timeout = null
        };
    }
    
    private static TestMetadata CreateTestMetadataWithDataSize(int size)
    {
        var testType = typeof(SampleTestClass);
        var dataSources = Enumerable.Range(0, size)
            .Select(i => new InlineDataSourceProvider(i, i * 2, i * 3))
            .Cast<IDataSourceProvider>()
            .ToArray();
        
        return new TestMetadata
        {
            TestIdTemplate = "Test_{TestIndex}",
            TestClassType = testType,
            TestMethod = testType.GetMethod("TestMethod")!,
            MethodMetadata = CreateMethodMetadata(testType),
            TestFilePath = "Test.cs",
            TestLineNumber = 1,
            TestClassFactory = args => new SampleTestClass(),
            ClassDataSources = Array.Empty<IDataSourceProvider>(),
            MethodDataSources = dataSources,
            PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
            DisplayNameTemplate = "Test",
            RepeatCount = 1,
            IsAsync = false,
            IsSkipped = false,
            Attributes = Array.Empty<Attribute>(),
            Timeout = null
        };
    }
    
    private static TestMetadata CreateTestMetadataWithMultipleDataSources()
    {
        var testType = typeof(ComplexTestClass);
        var propertyInfo = testType.GetProperty("TestProperty")!;
        
        return new TestMetadata
        {
            TestIdTemplate = "Test_{TestIndex}",
            TestClassType = testType,
            TestMethod = testType.GetMethod("TestMethod")!,
            MethodMetadata = CreateMethodMetadata(testType),
            TestFilePath = "Test.cs",
            TestLineNumber = 1,
            TestClassFactory = args => new ComplexTestClass((int)args![0]),
            ClassDataSources = new IDataSourceProvider[]
            {
                new InlineDataSourceProvider(1),
                new InlineDataSourceProvider(2)
            },
            MethodDataSources = new IDataSourceProvider[]
            {
                new InlineDataSourceProvider("a", "b"),
                new InlineDataSourceProvider("c", "d")
            },
            PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>
            {
                [propertyInfo] = new InlineDataSourceProvider(100)
            },
            DisplayNameTemplate = "Test",
            RepeatCount = 1,
            IsAsync = false,
            IsSkipped = false,
            Attributes = Array.Empty<Attribute>(),
            Timeout = null
        };
    }
    
    private static MethodMetadata CreateMethodMetadata(Type testType)
    {
        return new MethodMetadata
        {
            Name = "TestMethod",
            Type = testType,
            Parameters = Array.Empty<ParameterMetadata>(),
            GenericTypeCount = 0,
            Class = new ClassMetadata
            {
                Name = testType.Name,
                Type = testType,
                Attributes = Array.Empty<AttributeMetadata>(),
                Namespace = testType.Namespace ?? "",
                Assembly = new AssemblyMetadata 
                { 
                    Name = testType.Assembly.GetName().Name ?? "TestAssembly", 
                    Attributes = Array.Empty<AttributeMetadata>() 
                },
                Parameters = Array.Empty<ParameterMetadata>(),
                Properties = Array.Empty<PropertyMetadata>(),
                Constructors = Array.Empty<ConstructorMetadata>(),
                Parent = null
            },
            ReturnType = typeof(void),
            Attributes = Array.Empty<AttributeMetadata>()
        };
    }
    
    // Test classes for performance testing
    private class SampleTestClass
    {
        public void TestMethod(int a, int b, int c) { }
    }
    
    private class ComplexTestClass
    {
        public ComplexTestClass(int value) { }
        public int TestProperty { get; set; }
        public void TestMethod(string a, string b) { }
    }
    
    private class PerformanceTestClass
    {
        public void TestMethod(int number, string text, bool flag) { }
    }
}