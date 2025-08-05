using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Services;
using TUnit.Engine.Scheduling;

namespace TUnit.Engine;

internal sealed class TestDiscoveryResult
{
    public IEnumerable<AbstractExecutableTest> Tests { get; }
    public ExecutionContext? ExecutionContext { get; }

    public TestDiscoveryResult(IEnumerable<AbstractExecutableTest> tests, ExecutionContext? executionContext)
    {
        Tests = tests;
        ExecutionContext = executionContext;
    }
}

/// Unified test discovery service using the pipeline architecture with streaming support
internal sealed class TestDiscoveryService : IDataProducer
{
    private const int DiscoveryTimeoutSeconds = 60;
    private readonly HookOrchestrator _hookOrchestrator;
    private readonly TestBuilderPipeline _testBuilderPipeline;
    private readonly TestFilterService _testFilterService;
    private readonly ConcurrentBag<AbstractExecutableTest> _cachedTests =
    [
    ];
    private readonly TestDependencyResolver _dependencyResolver = new();

    public string Uid => "TUnit";
    public string Version => "2.0.0";
    public string DisplayName => "TUnit Test Discovery";
    public string Description => "TUnit Unified Test Discovery Service";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public TestDiscoveryService(HookOrchestrator hookOrchestrator, TestBuilderPipeline testBuilderPipeline, TestFilterService testFilterService)
    {
        _hookOrchestrator = hookOrchestrator;
        _testBuilderPipeline = testBuilderPipeline ?? throw new ArgumentNullException(nameof(testBuilderPipeline));
        _testFilterService = testFilterService;
    }

    public async Task<TestDiscoveryResult> DiscoverTests(string testSessionId, ITestExecutionFilter? filter, CancellationToken cancellationToken)
    {
        var beforeDiscoveryContext = await _hookOrchestrator.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken);
#if NET
        if (beforeDiscoveryContext != null)
        {
            ExecutionContext.Restore(beforeDiscoveryContext);
        }
#endif

        // Extract types from filter for optimized discovery
        var filterTypes = TestFilterTypeExtractor.ExtractTypesFromFilter(filter);

        var tests = new List<AbstractExecutableTest>();

        await foreach (var test in DiscoverTestsStreamAsync(testSessionId, filterTypes, cancellationToken))
        {
            tests.Add(test);
        }

        // Now that all tests are discovered, resolve dependencies
        foreach (var test in tests)
        {
            _dependencyResolver.TryResolveDependencies(test);
        }
        
        // Create execution plan which will detect circular dependencies
        var executionPlan = ExecutionPlan.Create(tests);
        
        // Apply filter first to get the tests we want to run
        var filteredTests = _testFilterService.FilterTests(filter, tests);

        // Now find all dependencies of filtered tests and add them
        var testsToInclude = new HashSet<AbstractExecutableTest>(filteredTests);
        var queue = new Queue<AbstractExecutableTest>(filteredTests);
        
        while (queue.Count > 0)
        {
            var test = queue.Dequeue();
            foreach (var resolvedDep in test.Dependencies)
            {
                var dependency = resolvedDep.Test;
                if (testsToInclude.Add(dependency))
                {
                    queue.Enqueue(dependency);
                }
            }
        }

        filteredTests = testsToInclude.ToList();

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

        // Register the filtered tests to invoke ITestRegisteredEventReceiver
        await _testFilterService.RegisterTestsAsync(filteredTests);

        // Capture the final execution context after discovery
        var finalContext = ExecutionContext.Capture();
        return new TestDiscoveryResult(filteredTests, finalContext);
    }

    /// Streams test discovery for parallel discovery and execution
    private async IAsyncEnumerable<AbstractExecutableTest> DiscoverTestsStreamAsync(
        string testSessionId,
        HashSet<Type>? filterTypes,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (!Debugger.IsAttached)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(DiscoveryTimeoutSeconds));
        }

        await foreach (var test in BuildTestsAsync(testSessionId, filterTypes, cts.Token))
        {
            _dependencyResolver.RegisterTest(test);

            // Cache for backward compatibility
            _cachedTests.Add(test);

            yield return test;
        }
    }


    private async IAsyncEnumerable<AbstractExecutableTest> BuildTestsAsync(string testSessionId,
        HashSet<Type>? filterTypes,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var executableTests = await _testBuilderPipeline.BuildTestsAsync(testSessionId, filterTypes);

        foreach (var test in executableTests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return test;
        }
    }



    public IEnumerable<TestContext> GetCachedTestContexts()
    {
        return _cachedTests.Select(t => t.Context);
    }
}
