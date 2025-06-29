using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;
using TUnit.Engine;
using TUnit.Engine.Building;
using TUnit.Engine.Framework;

namespace TUnit.Examples;

/// <summary>
/// Example showing how to use the unified test builder architecture
/// </summary>
public class UnifiedTestBuilderExample
{
    /// <summary>
    /// Example 1: Using the unified test builder in AOT mode (default)
    /// </summary>
    public static async Task ExampleAotMode()
    {
        // Setup services
        var services = new ServiceCollection();
        
        // Register test invoker and hook invoker (normally done by the framework)
        services.AddSingleton<ITestInvoker, DefaultTestInvoker>();
        services.AddSingleton<IHookInvoker, DefaultHookInvoker>();
        
        // Create a test metadata source (normally from source generation)
        var metadataSource = new SourceGeneratedTestMetadataSource(() => GetSampleTestMetadata());
        
        // Register the unified test builder for AOT mode
        services.AddUnifiedTestBuilderAot(metadataSource);
        
        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Get the test discovery service
        var discoveryService = serviceProvider.GetRequiredService<TestDiscoveryServiceV2>();
        
        // Discover all tests
        var tests = await discoveryService.DiscoverTests();
        
        Console.WriteLine($"Discovered {tests.Count()} tests in AOT mode");
        foreach (var test in tests)
        {
            Console.WriteLine($"  - {test.DisplayName} [{test.TestId}]");
        }
    }
    
    /// <summary>
    /// Example 2: Using the unified test builder in reflection mode
    /// </summary>
    public static async Task ExampleReflectionMode()
    {
        // Setup services
        var services = new ServiceCollection();
        
        // Register test invoker and hook invoker
        services.AddSingleton<ITestInvoker, DefaultTestInvoker>();
        services.AddSingleton<IHookInvoker, DefaultHookInvoker>();
        
        // Get assemblies to scan
        var assemblies = new[] { typeof(SampleTestClass).Assembly };
        
        // Register the unified test builder for reflection mode
        services.AddUnifiedTestBuilderReflection(assemblies);
        
        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Get the test discovery service
        var discoveryService = serviceProvider.GetRequiredService<TestDiscoveryServiceV2>();
        
        // Discover all tests
        var tests = await discoveryService.DiscoverTests();
        
        Console.WriteLine($"Discovered {tests.Count()} tests in reflection mode");
        foreach (var test in tests)
        {
            Console.WriteLine($"  - {test.DisplayName} [{test.TestId}]");
        }
    }
    
    /// <summary>
    /// Example 3: Using the pipeline directly
    /// </summary>
    public static async Task ExampleDirectPipeline()
    {
        // Create metadata source
        var metadataSource = new SourceGeneratedTestMetadataSource(() => GetSampleTestMetadata());
        
        // Create test and hook invokers
        var testInvoker = new DefaultTestInvoker();
        var hookInvoker = new DefaultHookInvoker();
        
        // Create the pipeline for AOT mode
        var pipeline = UnifiedTestBuilderPipelineFactory.CreateAotPipeline(
            metadataSource, 
            testInvoker, 
            hookInvoker);
        
        // Build all tests
        var tests = await pipeline.BuildTestsAsync();
        
        Console.WriteLine($"Built {tests.Count()} tests using direct pipeline");
        foreach (var test in tests)
        {
            Console.WriteLine($"  - {test.DisplayName}");
            Console.WriteLine($"    ID: {test.TestId}");
            Console.WriteLine($"    Can run in parallel: {test.Metadata.CanRunInParallel}");
        }
    }
    
    /// <summary>
    /// Example 4: Data-driven tests with the new factory pattern
    /// </summary>
    public static async Task ExampleDataDrivenTests()
    {
        // Create test metadata with data sources
        var metadata = new TestMetadata
        {
            TestId = "ExampleTests.DataDrivenTest",
            TestName = "DataDrivenTest",
            TestClassType = typeof(SampleTestClass),
            TestMethodName = "TestWithData",
            Categories = new[] { "DataDriven" },
            IsSkipped = false,
            CanRunInParallel = true,
            DependsOn = Array.Empty<string>(),
            
            // Static data source with factory pattern
            DataSources = new TestDataSource[]
            {
                new StaticTestDataSource(
                    new object?[][] 
                    { 
                        new object?[] { 1, 2, 3 },
                        new object?[] { 4, 5, 9 },
                        new object?[] { 10, 20, 30 }
                    })
            },
            
            // Class-level data for constructor
            ClassDataSources = new TestDataSource[]
            {
                new StaticTestDataSource(
                    new object?[][] 
                    { 
                        new object?[] { "TestContext1" },
                        new object?[] { "TestContext2" }
                    })
            },
            
            // Property data sources (single values)
            PropertyDataSources = new PropertyDataSource[]
            {
                new PropertyDataSource
                {
                    PropertyName = "TestProperty",
                    PropertyType = typeof(string),
                    DataSource = new StaticTestDataSource(
                        new object?[][] 
                        { 
                            new object?[] { "PropertyValue1" },
                            new object?[] { "PropertyValue2" }
                        })
                }
            },
            
            ParameterCount = 3,
            ParameterTypes = new[] { typeof(int), typeof(int), typeof(int) },
            Hooks = new TestHooks(),
            InstanceFactory = args => new SampleTestClass((string)args[0]),
            TestInvoker = async (instance, args) => 
            {
                var method = instance.GetType().GetMethod("TestWithData");
                await Task.Run(() => method!.Invoke(instance, args));
            }
        };
        
        // Create a simple metadata source
        var metadataSource = new SourceGeneratedTestMetadataSource(() => new[] { metadata });
        
        // Create the pipeline
        var pipeline = UnifiedTestBuilderPipelineFactory.CreateAotPipeline(
            metadataSource,
            new DefaultTestInvoker(),
            new DefaultHookInvoker());
        
        // Build tests - this will expand all data combinations
        var tests = await pipeline.BuildTestsAsync();
        
        Console.WriteLine($"Expanded to {tests.Count()} test variations:");
        foreach (var test in tests)
        {
            Console.WriteLine($"  - {test.DisplayName}");
            
            // The key feature: each test gets fresh data instances
            var instance1 = await test.CreateInstance();
            var instance2 = await test.CreateInstance();
            
            Console.WriteLine($"    Instance 1: {instance1.GetHashCode()}");
            Console.WriteLine($"    Instance 2: {instance2.GetHashCode()}");
            Console.WriteLine($"    Instances are different: {!ReferenceEquals(instance1, instance2)}");
        }
    }
    
    // Sample test metadata for examples
    private static IEnumerable<TestMetadata> GetSampleTestMetadata()
    {
        return new[]
        {
            new TestMetadata
            {
                TestId = "SampleTests.Test1",
                TestName = "Test1",
                TestClassType = typeof(SampleTestClass),
                TestMethodName = "SimpleTest",
                Categories = Array.Empty<string>(),
                IsSkipped = false,
                CanRunInParallel = true,
                DependsOn = Array.Empty<string>(),
                DataSources = Array.Empty<TestDataSource>(),
                ClassDataSources = Array.Empty<TestDataSource>(),
                PropertyDataSources = Array.Empty<PropertyDataSource>(),
                ParameterCount = 0,
                ParameterTypes = Array.Empty<Type>(),
                Hooks = new TestHooks()
            },
            new TestMetadata
            {
                TestId = "SampleTests.Test2",
                TestName = "Test2",
                TestClassType = typeof(SampleTestClass),
                TestMethodName = "TestWithTimeout",
                Categories = new[] { "Integration" },
                IsSkipped = false,
                TimeoutMs = 5000,
                RetryCount = 2,
                CanRunInParallel = false,
                DependsOn = new[] { "SampleTests.Test1" },
                DataSources = Array.Empty<TestDataSource>(),
                ClassDataSources = Array.Empty<TestDataSource>(),
                PropertyDataSources = Array.Empty<PropertyDataSource>(),
                ParameterCount = 0,
                ParameterTypes = Array.Empty<Type>(),
                Hooks = new TestHooks()
            }
        };
    }
}

// Sample test class for examples
public class SampleTestClass
{
    private readonly string _context;
    
    public SampleTestClass() : this("default") { }
    
    public SampleTestClass(string context)
    {
        _context = context;
    }
    
    public string TestProperty { get; set; } = "";
    
    [Test]
    public void SimpleTest()
    {
        Console.WriteLine($"Running SimpleTest in context: {_context}");
    }
    
    [Test]
    [Timeout(5000)]
    [Retry(2)]
    [NotInParallel]
    [DependsOn("SimpleTest")]
    [Category("Integration")]
    public async Task TestWithTimeout()
    {
        Console.WriteLine($"Running TestWithTimeout in context: {_context}");
        await Task.Delay(100);
    }
    
    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(4, 5, 9)]
    public void TestWithData(int a, int b, int expected)
    {
        Console.WriteLine($"Testing {a} + {b} = {expected} in context: {_context}, property: {TestProperty}");
    }
}

// Placeholder implementations for the example
public class DefaultTestInvoker : ITestInvoker
{
    public Task InvokeTestMethod(object instance, MethodInfo method, object?[] arguments)
    {
        var result = method.Invoke(instance, arguments);
        if (result is Task task)
            return task;
        return Task.CompletedTask;
    }
}

public class DefaultHookInvoker : IHookInvoker
{
    public Task InvokeHookAsync(object? instance, MethodInfo method, HookContext context)
    {
        var result = method.Invoke(instance, new object[] { context });
        if (result is Task task)
            return task;
        return Task.CompletedTask;
    }
}