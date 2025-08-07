using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
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
        var discoveryContext = await _hookOrchestrator.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken);
#if NET
        if (discoveryContext != null)
        {
            ExecutionContext.Restore(discoveryContext);
        }
#endif

        // Extract types from filter for optimized discovery
        var filterTypes = TestFilterTypeExtractor.ExtractTypesFromFilter(filter);

        // Stage 1: Stream independent tests immediately while buffering dependent tests
        var independentTests = new List<AbstractExecutableTest>();
        var dependentTests = new List<AbstractExecutableTest>();
        var allTests = new List<AbstractExecutableTest>();

        await foreach (var test in DiscoverTestsStreamAsync(testSessionId, filterTypes, cancellationToken))
        {
            allTests.Add(test);
            
            // Check if this test has dependencies based on metadata
            if (test.Metadata.Dependencies.Length > 0)
            {
                // Buffer tests with dependencies for later resolution
                dependentTests.Add(test);
            }
            else
            {
                // Independent test - can be used immediately
                independentTests.Add(test);
            }
        }

        // Now resolve dependencies for dependent tests
        foreach (var test in dependentTests)
        {
            _dependencyResolver.TryResolveDependencies(test);
        }
        
        // Check for circular dependencies and mark failed tests
        _dependencyResolver.CheckForCircularDependencies();
        
        // Combine independent and dependent tests
        var tests = new List<AbstractExecutableTest>(independentTests.Count + dependentTests.Count);
        tests.AddRange(independentTests);
        tests.AddRange(dependentTests);
        
        // Create execution plan for ordering
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
        contextProvider.TestDiscoveryContext.AddTests(allTests.Select(t => t.Context));

        await _hookOrchestrator.ExecuteAfterTestDiscoveryHooksAsync(cancellationToken);

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
        // Always use streaming version now that it's implemented
        await foreach (var test in _testBuilderPipeline.BuildTestsStreamingAsync(testSessionId, filterTypes, cancellationToken))
        {
            yield return test;
        }
    }

    /// <summary>
    /// Truly streaming test discovery that yields independent tests immediately
    /// and progressively resolves dependent tests as their dependencies are satisfied
    /// </summary>
    public async IAsyncEnumerable<AbstractExecutableTest> DiscoverTestsFullyStreamingAsync(
        string testSessionId,
        ITestExecutionFilter? filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var discoveryContext = await _hookOrchestrator.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken);
#if NET
        if (discoveryContext != null)
        {
            ExecutionContext.Restore(discoveryContext);
        }
#endif

        // Extract types from filter for optimized discovery
        var filterTypes = TestFilterTypeExtractor.ExtractTypesFromFilter(filter);

        // Lightweight tracking structures
        var testIdToTest = new ConcurrentDictionary<string, AbstractExecutableTest>();
        var pendingDependentTests = new ConcurrentDictionary<string, AbstractExecutableTest>();
        var completedTests = new ConcurrentDictionary<string, bool>();
        var readyTestsChannel = Channel.CreateUnbounded<AbstractExecutableTest>();

        // Start discovery task that feeds the channel
        var discoveryTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var test in DiscoverTestsStreamAsync(testSessionId, filterTypes, cancellationToken))
                {
                    testIdToTest[test.TestId] = test;

                    // Check if this test has dependencies
                    if (test.Metadata.Dependencies.Length == 0)
                    {
                        // No dependencies - stream immediately
                        await readyTestsChannel.Writer.WriteAsync(test, cancellationToken);
                    }
                    else
                    {
                        // Has dependencies - buffer for later
                        pendingDependentTests[test.TestId] = test;
                    }
                }

                // Discovery complete - now resolve dependencies
                // This is still needed for dependent tests
                foreach (var test in pendingDependentTests.Values)
                {
                    _dependencyResolver.TryResolveDependencies(test);
                }

                // Check for circular dependencies
                _dependencyResolver.CheckForCircularDependencies();

                // Queue tests whose dependencies are already satisfied
                foreach (var test in pendingDependentTests.Values.ToList())
                {
                    if (AreAllDependenciesSatisfied(test, completedTests))
                    {
                        pendingDependentTests.TryRemove(test.TestId, out _);
                        await readyTestsChannel.Writer.WriteAsync(test, cancellationToken);
                    }
                }
            }
            finally
            {
                // Signal that discovery is complete
                readyTestsChannel.Writer.TryComplete();
            }
        }, cancellationToken);

        // Yield tests as they become ready
        await foreach (var test in readyTestsChannel.Reader.ReadAllAsync(cancellationToken))
        {
            // Apply filter
            if (_testFilterService.MatchesTest(filter, test))
            {
                yield return test;
            }

            // Mark test as completed for dependency resolution
            completedTests[test.TestId] = true;

            // Check if any pending tests now have their dependencies satisfied
            var nowReadyTests = new List<AbstractExecutableTest>();
            foreach (var pendingTest in pendingDependentTests.Values)
            {
                if (AreAllDependenciesSatisfied(pendingTest, completedTests))
                {
                    nowReadyTests.Add(pendingTest);
                }
            }

            // Queue newly ready tests
            foreach (var readyTest in nowReadyTests)
            {
                pendingDependentTests.TryRemove(readyTest.TestId, out _);
                await readyTestsChannel.Writer.WriteAsync(readyTest, cancellationToken);
            }
        }

        // Ensure discovery task completes
        await discoveryTask;
    }

    private bool AreAllDependenciesSatisfied(AbstractExecutableTest test, ConcurrentDictionary<string, bool> completedTests)
    {
        foreach (var dependency in test.Dependencies)
        {
            if (!completedTests.ContainsKey(dependency.Test.TestId))
            {
                return false;
            }
        }
        return true;
    }



    public IEnumerable<TestContext> GetCachedTestContexts()
    {
        return _cachedTests.Select(t => t.Context);
    }
}
