using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Streamlined test discovery service that directly loads test metadata from sources
/// </summary>
public sealed class TestDiscoveryService : ITestDiscoverer, IDataProducer
{
    private readonly ITestMetadataSource[] _sources;
    private readonly TestFactory _testFactory;
    private readonly bool _enableDynamicDiscovery;
    
    public string Uid => "TUnit";
    public string Version => "1.0.0";
    public string DisplayName => "TUnit Test Discovery";
    public string Description => "TUnit Test Discovery Service";
    public Type[] DataTypesProduced => new[] { typeof(TestNodeUpdateMessage) };
    
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    
    public TestDiscoveryService(
        ITestMetadataSource[] sources,
        TestFactory testFactory,
        bool enableDynamicDiscovery = false)
    {
        _sources = sources;
        _testFactory = testFactory;
        _enableDynamicDiscovery = enableDynamicDiscovery;
    }
    
    /// <summary>
    /// Discovers all tests from configured sources (AOT-safe when using source generation)
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> DiscoverTests()
    {
        // If dynamic discovery is disabled (source generation path), this is AOT-safe
        if (!_enableDynamicDiscovery)
        {
            return await DiscoverTestsAotSafe();
        }
        
        // Otherwise, use the reflection-based path
        #pragma warning disable IL3050 // Calling method with RequiresDynamicCodeAttribute
        #pragma warning disable IL2026 // Calling method with RequiresUnreferencedCodeAttribute
        return await DiscoverTestsWithReflection();
        #pragma warning restore IL2026
        #pragma warning restore IL3050
    }
    
    /// <summary>
    /// AOT-safe test discovery (source generation path only)
    /// </summary>
    private async Task<IEnumerable<ExecutableTest>> DiscoverTestsAotSafe()
    {
        const int DiscoveryTimeoutSeconds = 30;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(DiscoveryTimeoutSeconds));
        
        try
        {
            return await DiscoverTestsAotSafeWithTimeout(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Test discovery timed out after {DiscoveryTimeoutSeconds} seconds. " +
                "This may indicate an issue with data sources or excessive test generation.");
        }
    }
    
    /// <summary>
    /// Reflection-based test discovery (requires dynamic code)
    /// </summary>
    [RequiresDynamicCode("Reflection mode requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
    private async Task<IEnumerable<ExecutableTest>> DiscoverTestsWithReflection()
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
    
    /// <summary>
    /// AOT-safe test discovery implementation (no reflection)
    /// </summary>
    private async Task<IEnumerable<ExecutableTest>> DiscoverTestsAotSafeWithTimeout(CancellationToken cancellationToken)
    {
        var allTests = new List<ExecutableTest>();
        const int MaxTestsPerDiscovery = 50_000;
        
        // Load metadata from all sources (source-generated)
        var allMetadata = new List<TestMetadata>();
        foreach (var source in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var metadata = await source.GetTestMetadata();
            allMetadata.AddRange(metadata);
        }
        
        // Create executable tests from metadata
        foreach (var metadata in allMetadata)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var executableTests = await _testFactory.CreateTests(metadata);
                allTests.AddRange(executableTests);
            }
            catch (Exception ex)
            {
                // Create a failed test that will report the construction error
                var failedTest = CreateFailedTest(metadata, ex);
                allTests.Add(failedTest);
            }
            
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
            // Registry not initialized - this is a framework initialization issue
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
    
    [RequiresDynamicCode("Reflection mode requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
    private async Task<IEnumerable<ExecutableTest>> DiscoverTestsWithTimeout(CancellationToken cancellationToken)
    {
        var allTests = new List<ExecutableTest>();
        const int MaxTestsPerDiscovery = 50_000;
        
        // Load metadata from all sources
        var allMetadata = new List<TestMetadata>();
        foreach (var source in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var metadata = await source.GetTestMetadata();
            allMetadata.AddRange(metadata);
        }
        
        // Optionally discover tests via reflection (for dynamic scenarios)
        if (_enableDynamicDiscovery)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dynamicMetadata = await DiscoverTestsDynamically();
            allMetadata.AddRange(dynamicMetadata);
        }
        
        // Create executable tests from metadata
        foreach (var metadata in allMetadata)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var executableTests = await _testFactory.CreateTests(metadata);
                allTests.AddRange(executableTests);
            }
            catch (Exception ex)
            {
                // Create a failed test that will report the construction error
                var failedTest = CreateFailedTest(metadata, ex);
                allTests.Add(failedTest);
            }
            
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
            // Registry not initialized - this is a framework initialization issue
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
    
    /// <summary>
    /// Implementation of ITestDiscoverer for Microsoft.Testing.Platform integration
    /// </summary>
    public async Task<IEnumerable<TestNode>> DiscoverTestsAsync(
        DiscoverTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        var tests = await DiscoverTests();
        var testNodes = new List<TestNode>();
        
        foreach (var test in tests)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            var node = CreateTestNode(test);
            testNodes.Add(node);
            
            // Report discovery progress
            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(
                    request.Session.SessionUid,
                    node));
        }
        
        return testNodes;
    }
    
    private TestNode CreateTestNode(ExecutableTest test)
    {
        var properties = new PropertyBag();
        
        // Add standard properties
        properties.Add(new KeyValuePairStringProperty(TestNodeProperties.FullyQualifiedName, test.TestId));
        properties.Add(new KeyValuePairStringProperty(TestNodeProperties.DisplayName, test.DisplayName));
        
        // Add location info if available
        if (test.Metadata.FilePath != null)
        {
            properties.Add(new TestFileLocationProperty(
                    test.Metadata.FilePath,
                    new LinePositionSpan(
                        new LinePosition(test.Metadata.LineNumber ?? 0, 0),
                        new LinePosition(test.Metadata.LineNumber ?? 0, 0))));
        }
        
        // Add categories
        if (test.Metadata.Categories.Length > 0)
        {
            foreach (var category in test.Metadata.Categories)
            {
                properties.Add(new KeyValuePairStringProperty("Category", category));
            }
        }
        
        return new TestNode
        {
            Uid = new TestNodeUid(test.TestId),
            DisplayName = test.DisplayName,
            Properties = properties
        };
    }
    
    private void ResolveDependencies(List<ExecutableTest> tests)
    {
        var testMap = tests.ToDictionary(t => t.TestId);
        
        foreach (var test in tests)
        {
            var dependencies = new List<ExecutableTest>();
            
            foreach (var dependencyId in test.Metadata.DependsOn)
            {
                if (testMap.TryGetValue(dependencyId, out var dependency))
                {
                    dependencies.Add(dependency);
                }
                else
                {
                    // Handle missing dependency - could log warning or fail
                }
            }
            
            test.Dependencies = dependencies.ToArray();
        }
    }
    
    /// <summary>
    /// Creates a failed test entry for tests that couldn't be constructed
    /// </summary>
    private ExecutableTest CreateFailedTest(TestMetadata metadata, Exception constructionException)
    {
        var testId = metadata.TestId ?? $"{metadata.TestClassType.FullName}.{metadata.TestMethodName}";
        var displayName = metadata.TestName ?? testId;
        
        // Add error information to the display name
        displayName = $"{displayName} [FAILED TO CONSTRUCT]";
        
        return new ExecutableTest
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = Array.Empty<object?>(),
            CreateInstance = () => 
            {
                // Return a dummy instance - we won't actually use it
                return Task.FromResult<object>(new object());
            },
            InvokeTest = (instance) => 
            {
                // This test always fails with the construction error
                throw new InvalidOperationException(
                    $"Test construction failed: {constructionException.Message}", 
                    constructionException);
            },
            Hooks = new TestLifecycleHooks
            {
                BeforeClass = Array.Empty<Func<HookContext, Task>>(),
                AfterClass = Array.Empty<Func<object, HookContext, Task>>(),
                BeforeTest = Array.Empty<Func<object, HookContext, Task>>(),
                AfterTest = Array.Empty<Func<object, HookContext, Task>>()
            },
            PropertyValues = new Dictionary<string, object?>()
        };
    }
    
    private Task<IEnumerable<TestMetadata>> DiscoverTestsDynamically()
    {
        // This would use reflection to find tests in assemblies
        // Only used for dynamic scenarios where source generation isn't available
        return Task.FromResult<IEnumerable<TestMetadata>>(Array.Empty<TestMetadata>());
    }
}