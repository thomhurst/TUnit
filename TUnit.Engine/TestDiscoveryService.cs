using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Services;

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
    private readonly TestExecutor _testExecutor;
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

    public TestDiscoveryService(TestExecutor testExecutor, TestBuilderPipeline testBuilderPipeline, TestFilterService testFilterService)
    {
        _testExecutor = testExecutor;
        _testBuilderPipeline = testBuilderPipeline ?? throw new ArgumentNullException(nameof(testBuilderPipeline));
        _testFilterService = testFilterService;
    }

    public async Task<TestDiscoveryResult> DiscoverTests(string testSessionId, ITestExecutionFilter? filter, CancellationToken cancellationToken, bool isForExecution)
    {
        await _testExecutor.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

        var contextProvider = _testExecutor.GetContextProvider();

        contextProvider.BeforeTestDiscoveryContext.RestoreExecutionContext();

        // Create building context without filter to ensure all tests (including dependencies) are discovered
        // Filtering will be applied later after dependency resolution
        var buildingContext = new Building.TestBuildingContext(isForExecution, Filter: null);

        // Stage 1: Stream independent tests immediately while buffering dependent tests
        var independentTests = new List<AbstractExecutableTest>();
        var dependentTests = new List<AbstractExecutableTest>();
        var allTests = new List<AbstractExecutableTest>();

        await foreach (var test in DiscoverTestsStreamAsync(testSessionId, buildingContext, cancellationToken).ConfigureAwait(false))
        {
            allTests.Add(test);

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

        // Combine independent and dependent tests
        var tests = new List<AbstractExecutableTest>(independentTests.Count + dependentTests.Count);
        tests.AddRange(independentTests);
        tests.AddRange(dependentTests);

        // For discovery requests (IDE test explorers), return all tests including explicit ones
        // For execution requests, apply filtering to exclude explicit tests unless explicitly targeted
        var filteredTests = isForExecution ? _testFilterService.FilterTests(filter, tests) : tests;

        // If we applied filtering, find all dependencies of filtered tests and add them
        if (isForExecution)
        {
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
        }

        contextProvider.TestDiscoveryContext.AddTests(allTests.Select(static t => t.Context));

        await _testExecutor.ExecuteAfterTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

        contextProvider.TestDiscoveryContext.RestoreExecutionContext();

        // Register the filtered tests to invoke ITestRegisteredEventReceiver
        await _testFilterService.RegisterTestsAsync(filteredTests).ConfigureAwait(false);

        // Capture the final execution context after discovery
        var finalContext = ExecutionContext.Capture();
        return new TestDiscoveryResult(filteredTests, finalContext);
    }

    /// Streams test discovery for parallel discovery and execution
    private async IAsyncEnumerable<AbstractExecutableTest> DiscoverTestsStreamAsync(
        string testSessionId,
        Building.TestBuildingContext buildingContext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Set a reasonable timeout for test discovery (5 minutes)
        cts.CancelAfter(TimeSpan.FromMinutes(5));

        var tests = await _testBuilderPipeline.BuildTestsStreamingAsync(testSessionId, buildingContext, cancellationToken).ConfigureAwait(false);

        foreach (var test in tests)
        {
            _dependencyResolver.RegisterTest(test);

            // Cache for backward compatibility
            _cachedTests.Add(test);

            yield return test;
        }
    }

    /// <summary>
    /// Simplified streaming test discovery without channels - matches source generation approach
    /// </summary>
    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Generic test instantiation requires MakeGenericType")]
    #endif
    public async IAsyncEnumerable<AbstractExecutableTest> DiscoverTestsFullyStreamingAsync(
        string testSessionId,
        ITestExecutionFilter? filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _testExecutor.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

        // Create building context - this is for discovery/streaming, not execution filtering
        var buildingContext = new Building.TestBuildingContext(IsForExecution: false, Filter: null);

        // Collect all tests first (like source generation mode does)
        var allTests = new List<AbstractExecutableTest>();
        await foreach (var test in DiscoverTestsStreamAsync(testSessionId, buildingContext, cancellationToken).ConfigureAwait(false))
        {
            allTests.Add(test);
        }

        // Resolve dependencies for all tests
        foreach (var test in allTests)
        {
            _dependencyResolver.TryResolveDependencies(test);
        }

        // Separate into independent and dependent tests
        var independentTests = new List<AbstractExecutableTest>();
        var dependentTests = new List<AbstractExecutableTest>();

        foreach (var test in allTests)
        {
            if (test.Dependencies.Length == 0)
            {
                independentTests.Add(test);
            }
            else
            {
                dependentTests.Add(test);
            }
        }

        // Yield independent tests first
        foreach (var test in independentTests)
        {
            if (_testFilterService.MatchesTest(filter, test))
            {
                yield return test;
            }
        }

        // Process dependent tests in dependency order
        var yieldedTests = new HashSet<string>(independentTests.Select(static t => t.TestId));
        var remainingTests = new List<AbstractExecutableTest>(dependentTests);

        while (remainingTests.Count > 0)
        {
            var readyTests = new List<AbstractExecutableTest>();

            foreach (var test in remainingTests)
            {
                var allDependenciesYielded = test.Dependencies.All(dep => yieldedTests.Contains(dep.Test.TestId));

                if (allDependenciesYielded)
                {
                    readyTests.Add(test);
                }
            }

            // If no tests are ready, we have a circular dependency
            if (readyTests.Count == 0 && remainingTests.Count > 0)
            {
                // Yield remaining tests anyway to avoid hanging
                foreach (var test in remainingTests)
                {
                    if (_testFilterService.MatchesTest(filter, test))
                    {
                        yield return test;
                    }
                }
                break;
            }

            // Yield ready tests and remove from remaining
            foreach (var test in readyTests)
            {
                if (_testFilterService.MatchesTest(filter, test))
                {
                    yield return test;
                }
                yieldedTests.Add(test.TestId);
                remainingTests.Remove(test);
            }
        }
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
        return _cachedTests.Select(static t => t.Context);
    }
}
