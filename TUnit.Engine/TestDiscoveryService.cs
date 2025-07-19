using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Result of test discovery including tests and execution context
/// </summary>
internal sealed class TestDiscoveryResult
{
    public IEnumerable<ExecutableTest> Tests { get; }
    public ExecutionContext? ExecutionContext { get; }

    public TestDiscoveryResult(IEnumerable<ExecutableTest> tests, ExecutionContext? executionContext)
    {
        Tests = tests;
        ExecutionContext = executionContext;
    }
}

/// <summary>
/// Unified test discovery service that uses the new pipeline architecture
/// </summary>
internal sealed class TestDiscoveryService : IDataProducer
{
    private const int DiscoveryTimeoutSeconds = 60;
    private readonly HookOrchestrator _hookOrchestrator;
    private readonly UnifiedTestBuilderPipeline _testBuilderPipeline;
    private readonly TestFilterService _testFilterService;
    private readonly ConcurrentBag<ExecutableTest> _cachedTests = new();
    private readonly TestDependencyResolver _dependencyResolver = new();

    public string Uid => "TUnit";
    public string Version => "2.0.0";
    public string DisplayName => "TUnit Test Discovery";
    public string Description => "TUnit Unified Test Discovery Service";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public TestDiscoveryService(HookOrchestrator hookOrchestrator, UnifiedTestBuilderPipeline testBuilderPipeline, TestFilterService testFilterService)
    {
        _hookOrchestrator = hookOrchestrator;
        _testBuilderPipeline = testBuilderPipeline ?? throw new ArgumentNullException(nameof(testBuilderPipeline));
        _testFilterService = testFilterService;
    }

    /// <summary>
    /// Discovers all tests using the unified pipeline
    /// </summary>
    public async Task<TestDiscoveryResult> DiscoverTests(string testSessionId, ITestExecutionFilter? filter, CancellationToken cancellationToken)
    {
        var beforeDiscoveryContext = await _hookOrchestrator.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken);
#if NET
        if (beforeDiscoveryContext != null)
        {
            ExecutionContext.Restore(beforeDiscoveryContext);
        }
#endif

        var tests = new List<ExecutableTest>();

        await foreach (var test in DiscoverTestsStreamAsync(testSessionId, cancellationToken))
        {
            tests.Add(test);
        }

        // Populate the TestDiscoveryContext with all discovered tests before running AfterTestDiscovery hooks
        var contextProvider = _hookOrchestrator.GetContextProvider();
        contextProvider.TestDiscoveryContext.AddTests(tests.Select(t => t.Context));

        var afterDiscoveryContext = await _hookOrchestrator.ExecuteAfterTestDiscoveryHooksAsync(cancellationToken);
#if NET
        if (afterDiscoveryContext != null)
        {
            ExecutionContext.Restore(afterDiscoveryContext);
        }
#endif

        var filteredTests = _testFilterService.FilterTests(filter, tests);

        // Register the filtered tests to invoke ITestRegisteredEventReceiver
        await _testFilterService.RegisterTestsAsync(filteredTests);

        // Capture the final execution context after discovery
        var finalContext = ExecutionContext.Capture();
        return new TestDiscoveryResult(filteredTests, finalContext);
    }

    /// <summary>
    /// Discovers tests as a stream, enabling parallel discovery and execution
    /// </summary>
    private async IAsyncEnumerable<ExecutableTest> DiscoverTestsStreamAsync(
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (!Debugger.IsAttached)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(DiscoveryTimeoutSeconds));
        }

        await foreach (var test in BuildTestsAsync(testSessionId, cts.Token))
        {
            _dependencyResolver.RegisterTest(test);

            // Try to resolve dependencies immediately
            if (!_dependencyResolver.TryResolveDependencies(test) && test.Metadata.Dependencies.Length > 0)
            {
                // Mark as waiting if dependencies not ready
                test.State = TestState.WaitingForDependencies;
            }

            // Cache for backward compatibility
            _cachedTests.Add(test);

            yield return test;
        }
    }


    private async IAsyncEnumerable<ExecutableTest> BuildTestsAsync(string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var executableTests = await _testBuilderPipeline.BuildTestsAsync(testSessionId);

        foreach (var test in executableTests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return test;
        }
    }



    /// <summary>
    /// Gets all cached test contexts for use by TestFinder
    /// </summary>
    public IEnumerable<TestContext> GetCachedTestContexts()
    {
        return _cachedTests.Select(t => t.Context);
    }
}
