using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Unified test discovery service that uses the new pipeline architecture
/// </summary>
public sealed class TestDiscoveryServiceV2 : ITestDiscoverer, IDataProducer
{
    private readonly UnifiedTestBuilderPipeline _testBuilderPipeline;
    private readonly bool _enableDynamicDiscovery;
    
    public string Uid => "TUnit";
    public string Version => "2.0.0";
    public string DisplayName => "TUnit Test Discovery";
    public string Description => "TUnit Unified Test Discovery Service";
    public Type[] DataTypesProduced => new[] { typeof(TestNodeUpdateMessage) };
    
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    
    public TestDiscoveryServiceV2(
        UnifiedTestBuilderPipeline testBuilderPipeline,
        bool enableDynamicDiscovery = false)
    {
        _testBuilderPipeline = testBuilderPipeline ?? throw new ArgumentNullException(nameof(testBuilderPipeline));
        _enableDynamicDiscovery = enableDynamicDiscovery;
    }
    
    /// <summary>
    /// Discovers all tests using the unified pipeline
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> DiscoverTests()
    {
        const int DiscoveryTimeoutSeconds = 30;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(DiscoveryTimeoutSeconds));
        
        try
        {
            return await DiscoverTestsWithTimeout(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Test discovery timed out after {DiscoveryTimeoutSeconds} seconds. " +
                "This may indicate an issue with data sources or excessive test generation.");
        }
    }
    
    private async Task<IEnumerable<ExecutableTest>> DiscoverTestsWithTimeout(CancellationToken cancellationToken)
    {
        const int MaxTestsPerDiscovery = 50_000;
        
        // Use the pipeline to build all tests
        var allTests = new List<ExecutableTest>();
        
        await foreach (var test in BuildTestsAsync(cancellationToken))
        {
            allTests.Add(test);
            
            if (allTests.Count > MaxTestsPerDiscovery)
            {
                throw new InvalidOperationException(
                    $"Test discovery exceeded maximum test count of {MaxTestsPerDiscovery:N0}. " +
                    "Consider reducing data source sizes or using test filters.");
            }
        }
        
        // Register all discovered tests with the registry
        TestRegistry registry;
        try
        {
            registry = TestRegistry.Instance;
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                "TestRegistry has not been initialized. This usually indicates the test framework " +
                "has not been properly set up. Ensure TUnit framework initialization completes " +
                "before running discovery.", ex);
        }
        
        foreach (var test in allTests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            registry.RegisterTest(test);
        }
        
        // Resolve dependencies between tests
        ResolveDependencies(allTests);
        
        return allTests;
    }
    
    private async IAsyncEnumerable<ExecutableTest> BuildTestsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var executableTests = await _testBuilderPipeline.BuildTestsAsync();
        
        foreach (var test in executableTests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return test;
        }
    }
    
    private void ResolveDependencies(List<ExecutableTest> allTests)
    {
        var testMap = allTests.ToDictionary(t => t.TestId);
        
        foreach (var test in allTests)
        {
            var dependencies = new List<ExecutableTest>();
            
            foreach (var dependencyName in test.Metadata.DependsOn)
            {
                // Try exact match first
                if (testMap.TryGetValue(dependencyName, out var dependency))
                {
                    dependencies.Add(dependency);
                    continue;
                }
                
                // Try matching by test name
                var matchingTests = allTests.Where(t => 
                    t.Metadata.TestName == dependencyName ||
                    t.DisplayName == dependencyName).ToList();
                
                if (matchingTests.Count == 1)
                {
                    dependencies.Add(matchingTests[0]);
                }
                else if (matchingTests.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"Test '{test.DisplayName}' depends on '{dependencyName}' which matches multiple tests. " +
                        "Use a more specific test identifier.");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Test '{test.DisplayName}' depends on '{dependencyName}' which was not found.");
                }
            }
            
            test.Dependencies = dependencies.ToArray();
        }
    }
    
    /// <summary>
    /// ITestDiscoverer implementation for Microsoft.Testing.Platform
    /// </summary>
    public async Task DiscoverTestsAsync(
        DiscoverTestExecutionRequest discoverTestExecutionRequest,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        var tests = await DiscoverTests();
        
        foreach (var test in tests)
        {
            var testNode = CreateTestNode(test);
            var message = new TestNodeUpdateMessage(
                discoverTestExecutionRequest.Session.SessionUid,
                testNode);
            
            await messageBus.PublishAsync(this, message);
        }
    }
    
    private static TestNode CreateTestNode(ExecutableTest test)
    {
        var properties = new List<IProperty>();
        
        // Add standard properties
        properties.Add(new Property<string>("TestClass", test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name));
        properties.Add(new Property<string>("TestMethod", test.Metadata.TestMethodName));
        
        if (test.Metadata.FilePath != null)
        {
            properties.Add(new Property<string>("FilePath", test.Metadata.FilePath));
        }
        
        if (test.Metadata.LineNumber.HasValue)
        {
            properties.Add(new Property<int>("LineNumber", test.Metadata.LineNumber.Value));
        }
        
        // Add categories
        foreach (var category in test.Metadata.Categories)
        {
            properties.Add(new Property<string>("Category", category));
        }
        
        // Add skip information
        if (test.Metadata.IsSkipped)
        {
            properties.Add(new Property<bool>("IsSkipped", true));
            if (!string.IsNullOrEmpty(test.Metadata.SkipReason))
            {
                properties.Add(new Property<string>("SkipReason", test.Metadata.SkipReason));
            }
        }
        
        return new TestNode
        {
            Uid = new TestNodeUid(test.TestId),
            DisplayName = test.DisplayName,
            Properties = properties.ToArray()
        };
    }
}