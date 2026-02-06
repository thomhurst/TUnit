using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Building.Interfaces;
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
    private readonly MetadataDependencyExpander _dependencyExpander;
    private readonly ConcurrentBag<AbstractExecutableTest> _cachedTests = [];
    private readonly TestDependencyResolver _dependencyResolver = new();

    public string Uid => "TUnit";
    public string Version => "2.0.0";
    public string DisplayName => "TUnit Test Discovery";
    public string Description => "TUnit Unified Test Discovery Service";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public TestDiscoveryService(
        TestExecutor testExecutor,
        TestBuilderPipeline testBuilderPipeline,
        TestFilterService testFilterService,
        MetadataDependencyExpander dependencyExpander)
    {
        _testExecutor = testExecutor;
        _testBuilderPipeline = testBuilderPipeline ?? throw new ArgumentNullException(nameof(testBuilderPipeline));
        _testFilterService = testFilterService;
        _dependencyExpander = dependencyExpander ?? throw new ArgumentNullException(nameof(dependencyExpander));
    }

    public async Task<TestDiscoveryResult> DiscoverTests(string testSessionId, ITestExecutionFilter? filter, CancellationToken cancellationToken, bool isForExecution)
    {
        await _testExecutor.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

        var contextProvider = _testExecutor.GetContextProvider();
        contextProvider.BeforeTestDiscoveryContext.RestoreExecutionContext();

        var allTests = new List<AbstractExecutableTest>();

#pragma warning disable TPEXP
        var isNopFilter = filter is NopFilter;
#pragma warning restore TPEXP
        if (filter == null || !isForExecution || isNopFilter)
        {
            var buildingContext = new Building.TestBuildingContext(isForExecution, Filter: null);
            await foreach (var test in DiscoverTestsStreamAsync(testSessionId, buildingContext, cancellationToken).ConfigureAwait(false))
            {
                allTests.Add(test);
            }
        }
        else
        {
            // Use filter-aware collection to pre-filter by type before materializing all metadata
            var allMetadata = await _testBuilderPipeline.CollectTestMetadataAsync(testSessionId, filter).ConfigureAwait(false);
            var allMetadataList = allMetadata.ToList();

            var metadataToInclude = _dependencyExpander.ExpandToIncludeDependencies(allMetadataList, filter);

            // Build tests directly from the pre-collected metadata (avoid re-collecting)
            var buildingContext = new Building.TestBuildingContext(isForExecution, Filter: null);
            var tests = await _testBuilderPipeline.BuildTestsFromMetadataAsync(
                metadataToInclude,
                buildingContext,
                cancellationToken).ConfigureAwait(false);

            var testsList = tests.ToList();

            // Cache tests so ITestFinder can locate them
            foreach (var test in testsList)
            {
                _cachedTests.Add(test);
            }

            allTests.AddRange(testsList);
        }

        foreach (var test in allTests)
        {
            _dependencyResolver.RegisterTest(test);
        }

        foreach (var test in allTests.Where(t => t.Metadata.Dependencies.Length > 0))
        {
            _dependencyResolver.TryResolveDependencies(test);
        }

        // Populate TestContext._dependencies for ALL tests before After(TestDiscovery) hooks run.
        // This ensures hooks can access dependency information on any TestContext (including focused tests).
        _testBuilderPipeline.PopulateAllDependencies(allTests);

        // Add tests to context and run After(TestDiscovery) hooks before event receivers
        // This marks the end of the discovery phase, before registration begins
        contextProvider.TestDiscoveryContext.AddTests(allTests.Select(static t => t.Context));
        await _testExecutor.ExecuteAfterTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);
        contextProvider.TestDiscoveryContext.RestoreExecutionContext();

        // Now invoke event receivers (registration phase)
        // ITestRegisteredEventReceiver can access dependency information and any state set by After(TestDiscovery) hooks
        await InvokePostResolutionEventsInParallelAsync(allTests).ConfigureAwait(false);

        var filteredTests = isForExecution ? _testFilterService.FilterTests(filter, allTests) : allTests;

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

            filteredTests = [.. testsToInclude];
        }

        await _testFilterService.RegisterTestsAsync(filteredTests).ConfigureAwait(false);

        var finalContext = ExecutionContext.Capture();
        return new TestDiscoveryResult(filteredTests, finalContext);
    }

    private async IAsyncEnumerable<AbstractExecutableTest> DiscoverTestsStreamAsync(
        string testSessionId,
        Building.TestBuildingContext buildingContext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMinutes(5));

        var tests = await _testBuilderPipeline.BuildTestsStreamingAsync(testSessionId, buildingContext, metadataFilter: null, cancellationToken).ConfigureAwait(false);

        foreach (var test in tests)
        {
            _dependencyResolver.RegisterTest(test);
            _cachedTests.Add(test);
            yield return test;
        }
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Generic test instantiation requires MakeGenericType")]
    #endif
    public async IAsyncEnumerable<AbstractExecutableTest> DiscoverTestsFullyStreamingAsync(
        string testSessionId,
        ITestExecutionFilter? filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _testExecutor.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

        var buildingContext = new Building.TestBuildingContext(IsForExecution: false, Filter: null);

        var allTests = new List<AbstractExecutableTest>();
        await foreach (var test in DiscoverTestsStreamAsync(testSessionId, buildingContext, cancellationToken).ConfigureAwait(false))
        {
            allTests.Add(test);
        }

        foreach (var test in allTests)
        {
            _dependencyResolver.TryResolveDependencies(test);
        }

        // Populate TestContext._dependencies for ALL tests before After(TestDiscovery) hooks run.
        // This ensures hooks can access dependency information on any TestContext (including focused tests).
        _testBuilderPipeline.PopulateAllDependencies(allTests);

        // Add tests to context and run After(TestDiscovery) hooks before event receivers
        // This marks the end of the discovery phase, before registration begins
        var contextProvider = _testExecutor.GetContextProvider();
        contextProvider.TestDiscoveryContext.AddTests(allTests.Select(static t => t.Context));
        await _testExecutor.ExecuteAfterTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);
        contextProvider.TestDiscoveryContext.RestoreExecutionContext();

        // Now invoke event receivers (registration phase)
        // ITestRegisteredEventReceiver can access dependency information and any state set by After(TestDiscovery) hooks
        await InvokePostResolutionEventsInParallelAsync(allTests).ConfigureAwait(false);

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

        foreach (var test in independentTests)
        {
            if (_testFilterService.MatchesTest(filter, test))
            {
                yield return test;
            }
        }

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

            if (readyTests.Count == 0 && remainingTests.Count > 0)
            {
                foreach (var test in remainingTests)
                {
                    if (_testFilterService.MatchesTest(filter, test))
                    {
                        yield return test;
                    }
                }
                break;
            }

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



    private async Task InvokePostResolutionEventsInParallelAsync(List<AbstractExecutableTest> allTests)
    {
        if (allTests.Count < Building.ParallelThresholds.MinItemsForParallel)
        {
            foreach (var test in allTests)
            {
                await _testBuilderPipeline.InvokePostResolutionEventsAsync(test).ConfigureAwait(false);
            }
            return;
        }

#if NET6_0_OR_GREATER
        await Parallel.ForEachAsync(
            allTests,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            async (test, _) =>
            {
                await _testBuilderPipeline.InvokePostResolutionEventsAsync(test).ConfigureAwait(false);
            }
        ).ConfigureAwait(false);
#else
        var tasks = new Task[allTests.Count];
        for (var i = 0; i < allTests.Count; i++)
        {
            tasks[i] = _testBuilderPipeline.InvokePostResolutionEventsAsync(allTests[i]).AsTask();
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
    }

    public IEnumerable<TestContext> GetCachedTestContexts()
    {
        return _cachedTests.Select(static t => t.Context);
    }
}
