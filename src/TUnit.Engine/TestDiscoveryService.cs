using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Building;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Constants;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine;

internal sealed class TestDiscoveryResult
{
    public List<AbstractExecutableTest> Tests { get; }
    public ExecutionContext? ExecutionContext { get; }

    public TestDiscoveryResult(List<AbstractExecutableTest> tests, ExecutionContext? executionContext)
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
    private readonly ConcurrentQueue<AbstractExecutableTest> _cachedTests = new();
    private readonly TestDependencyResolver _dependencyResolver = new();
    private readonly TUnitFrameworkLogger _logger;

    // Tests that hooks excluded from this run (and that no remaining test depends on). Hidden from
    // TestFinder so TestContext dependency lookups don't see tests that will never get a result.
    private HashSet<TestContext>? _hookExcludedTests;

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
        MetadataDependencyExpander dependencyExpander,
        TUnitFrameworkLogger logger)
    {
        _logger = logger;
        _testExecutor = testExecutor;
        _testBuilderPipeline = testBuilderPipeline ?? throw new ArgumentNullException(nameof(testBuilderPipeline));
        _testFilterService = testFilterService;
        _dependencyExpander = dependencyExpander ?? throw new ArgumentNullException(nameof(dependencyExpander));
    }

    public async Task<TestDiscoveryResult> DiscoverTests(string testSessionId, ITestExecutionFilter? filter, CancellationToken cancellationToken, bool isForExecution)
    {
        // Each request recomputes the hook exclusions; only execution requests populate them again, so a
        // discovery-only request never sees state left by an earlier request on this instance.
        _hookExcludedTests = null;

        await _testExecutor.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

#if NET
        // Start session activity early so discovery spans can parent under it.
        if (isForExecution)
        {
            _testExecutor.TryStartSessionActivity();
        }
#endif

        var contextProvider = _testExecutor.GetContextProvider();
        contextProvider.BeforeTestDiscoveryContext.RestoreExecutionContext();

        var allTests = await DiscoverAndResolveTestsAsync(testSessionId, filter, isForExecution, cancellationToken).ConfigureAwait(false);

        // Tests excluded by After(TestDiscovery) hooks are dropped for discovery and execution alike,
        // and their order is kept. allTests keeps every built test so RemoveExcludedTestsFromHookContexts
        // can prune the excluded ones from their class contexts below.
        var discoveredTests = await ApplyAfterDiscoveryHooksAsync(contextProvider, allTests, cancellationToken).ConfigureAwait(false);

        // Only allocated when a hook removes tests.
        var removedByHooks = isForExecution
            ? AddRemovedByHooks(null, contextProvider.TestDiscoveryContext.TestSet.Removed)
            : null;

        // Apply the filter during discovery too — not just execution — so `--list-tests
        // --treenode-filter X` lists exactly the tests a run with X would execute. Only filter
        // when a real filter is present: bare `--list-tests` and IDE Test Explorer discovery send
        // null/NopFilter and must keep listing everything (including [Explicit] tests, which
        // FilterTests would otherwise strip). Filtering is side-effect-free (MatchesTest only
        // compares paths/properties), so unlike argument registration it is safe outside execution.
#pragma warning disable TPEXP
        var hasRealFilter = filter is not (null or NopFilter);
#pragma warning restore TPEXP
        var filteredTests = (isForExecution || hasRealFilter)
            ? _testFilterService.FilterTests(filter, discoveredTests)
            : discoveredTests;

        var finalTests = isForExecution
            ? IncludeDependencies(filteredTests)
            : filteredTests as List<AbstractExecutableTest> ?? [.. filteredTests];

        if (isForExecution)
        {
            (finalTests, removedByHooks) = await ExecuteTestFilteringHooksAsync(contextProvider, finalTests, removedByHooks, cancellationToken).ConfigureAwait(false);

            if (removedByHooks is not null)
            {
                await ReportHookExclusionsAsync(removedByHooks, finalTests).ConfigureAwait(false);
            }
        }

        if (isForExecution && finalTests.Count != allTests.Count)
        {
            RemoveExcludedTestsFromHookContexts(allTests, finalTests);
        }

        await _testFilterService.RegisterTestsAsync(finalTests, isForExecution).ConfigureAwait(false);

        var finalContext = ExecutionContext.Capture();
        return new TestDiscoveryResult(finalTests, finalContext);
    }

    // Every built test was added to its ClassHookContext when its TestContext was created. Filters
    // that can only be evaluated after building (e.g. on properties added at discovery time) leave
    // tests in those contexts that will never run, so hooks would otherwise see them through
    // TestClasses/AllTests/TestCount and fire work for classes that are not part of the run.
    private static void RemoveExcludedTestsFromHookContexts(
        List<AbstractExecutableTest> allTests,
        List<AbstractExecutableTest> finalTests)
    {
        var testsToKeep = new HashSet<TestContext>(TUnit.Core.Helpers.ReferenceEqualityComparer<TestContext>.Instance);
        foreach (var test in finalTests)
        {
            testsToKeep.Add(test.Context);
        }

        var visitedClasses = new HashSet<ClassHookContext>(TUnit.Core.Helpers.ReferenceEqualityComparer<ClassHookContext>.Instance);
        foreach (var test in allTests)
        {
            var classContext = test.Context.ClassContext;
            if (!testsToKeep.Contains(test.Context) && visitedClasses.Add(classContext))
            {
                classContext.RetainTests(testsToKeep);
            }
        }
    }

    // Runs Before(TestFiltering) hooks on the tests that passed framework filtering, re-includes
    // dependencies of the tests they kept, then shows the final set to After(TestFiltering) hooks.
    private async ValueTask<(List<AbstractExecutableTest> Tests, HashSet<TestContext>? RemovedByHooks)> ExecuteTestFilteringHooksAsync(
        Core.Services.IContextProvider contextProvider,
        List<AbstractExecutableTest> tests,
        HashSet<TestContext>? removedByHooks,
        CancellationToken cancellationToken)
    {
        // No hooks: skip the stage so runs without TestFiltering hooks pay no extra allocations.
        if (!await _testExecutor.HasTestFilteringHooksAsync().ConfigureAwait(false))
        {
            return (tests, removedByHooks);
        }

        var filteringContext = contextProvider.TestFilteringContext;
        var filteringSet = filteringContext.TestSet;
        filteringSet.Initialize(ToContextList(tests));

        await _testExecutor.ExecuteBeforeTestFilteringHooksAsync(cancellationToken).ConfigureAwait(false);
        filteringContext.RestoreExecutionContext();
        filteringSet.Seal();

        if (filteringSet.IsModified)
        {
            removedByHooks = AddRemovedByHooks(removedByHooks, filteringSet.Removed);

            // As with framework filtering, a test that a kept test depends on still runs.
            tests = IncludeDependencies(ToExecutableTestList(filteringSet.Tests));
            filteringSet.Replace(ToContextList(tests));
        }
        else if (filteringSet.WasReordered)
        {
            tests = ToExecutableTestList(filteringSet.Tests);
        }

        await _testExecutor.ExecuteAfterTestFilteringHooksAsync(cancellationToken).ConfigureAwait(false);
        filteringContext.RestoreExecutionContext();

        return (tests, removedByHooks);
    }

    // Finishes discovery for the built tests: runs discovery event receivers, then After(TestDiscovery)
    // hooks, and returns the tests the hooks kept, in hook order. Shared by both discovery paths so
    // they can't drift apart.
    private async ValueTask<List<AbstractExecutableTest>> ApplyAfterDiscoveryHooksAsync(
        Core.Services.IContextProvider contextProvider,
        List<AbstractExecutableTest> allTests,
        CancellationToken cancellationToken)
    {
        // ITestDiscoveryEventReceivers (e.g. [Category], [Property], [NotInParallel], [Timeout]) run first,
        // on every built test, so After(TestDiscovery) hooks see complete metadata and any [DependsOn]
        // target that is excluded and later re-included still has its attributes applied.
        // ITestRegisteredEventReceiver runs later, at registration, and still sees what the hooks did.
        await InvokePostResolutionEventsInParallelAsync(allTests).ConfigureAwait(false);

        // This marks the end of the discovery phase, before registration begins.
        var discoveryContext = contextProvider.TestDiscoveryContext;
        var discoverySet = discoveryContext.TestSet;
        discoverySet.Initialize(ToContextList(allTests));
        await _testExecutor.ExecuteAfterTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);
        discoveryContext.RestoreExecutionContext();
        discoverySet.Seal();

        return discoverySet.IsModified || discoverySet.WasReordered
            ? ToExecutableTestList(discoverySet.Tests)
            : allTests;
    }

    // Collects the tests a stage's hooks removed (MutableTestSet records them, so nothing is diffed).
    private static HashSet<TestContext>? AddRemovedByHooks(HashSet<TestContext>? removedByHooks, IReadOnlyList<TestContext> removed)
    {
        if (removed.Count == 0)
        {
            return removedByHooks;
        }

        removedByHooks ??= new HashSet<TestContext>(TUnit.Core.Helpers.ReferenceEqualityComparer<TestContext>.Instance);
        foreach (var test in removed)
        {
            removedByHooks.Add(test);
        }

        return removedByHooks;
    }

    // A hook-excluded test that still runs was re-included as a [DependsOn] target: say so, since the
    // user asked for it to be excluded. Whatever stays excluded is hidden from TestFinder.
    private async ValueTask ReportHookExclusionsAsync(HashSet<TestContext> removedByHooks, List<AbstractExecutableTest> finalTests)
    {
        foreach (var test in finalTests)
        {
            if (removedByHooks.Remove(test.Context) && _logger.IsDebugEnabled)
            {
                await _logger.LogDebugAsync(
                    $"Test '{test.TestId}' was excluded by a hook but still runs because a remaining test depends on it ([DependsOn]).").ConfigureAwait(false);
            }
        }

        _hookExcludedTests = removedByHooks;
    }

    // Adds the transitive [DependsOn] targets of the given tests. Input order is preserved and
    // dependencies that were not already present are appended, so hook ordering survives.
    private static List<AbstractExecutableTest> IncludeDependencies(IReadOnlyCollection<AbstractExecutableTest> tests)
    {
        var result = new List<AbstractExecutableTest>(tests.Count);
        var testsToInclude = new HashSet<AbstractExecutableTest>();
        foreach (var test in tests)
        {
            if (testsToInclude.Add(test))
            {
                result.Add(test);
            }
        }

        for (var i = 0; i < result.Count; i++)
        {
            foreach (var resolvedDep in result[i].Dependencies)
            {
                var dependency = resolvedDep.Test;
                if (testsToInclude.Add(dependency))
                {
                    result.Add(dependency);
                }
            }
        }

        return result;
    }

    private static List<AbstractExecutableTest> ToExecutableTestList(IReadOnlyList<TestContext> contexts)
    {
        var tests = new List<AbstractExecutableTest>(contexts.Count);
        foreach (var context in contexts)
        {
            tests.Add(context.InternalExecutableTest);
        }

        return tests;
    }

    // Project the resolved tests onto their contexts into a pre-sized list.
    // MutableTestSet stores its argument as-is, so a List<TestContext> costs a single pre-sized
    // allocation instead of a lazy Select() enumerator plus a ToArray() copy.
    private static List<TestContext> ToContextList(List<AbstractExecutableTest> tests)
    {
        var contexts = new List<TestContext>(tests.Count);
        foreach (var test in tests)
        {
            contexts.Add(test.Context);
        }

        return contexts;
    }

    private async Task<List<AbstractExecutableTest>> DiscoverAndResolveTestsAsync(
        string testSessionId,
        ITestExecutionFilter? filter,
        bool isForExecution,
        CancellationToken cancellationToken)
    {
#if NET
        Activity? discoveryActivity = null;
        if (TUnitActivitySource.LifecycleSource.HasListeners())
        {
            var sessionActivity = _testExecutor.GetContextProvider().TestSessionContext.Activity;
            discoveryActivity = TUnitActivitySource.StartLifecycleActivity(
                "test discovery",
                ActivityKind.Internal,
                sessionActivity?.Context ?? default);
        }
#endif

        var allTests = new List<AbstractExecutableTest>();

        try
        {
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

                // Apply 5-minute discovery timeout matching the streaming path (#4715)
                using var filterCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                filterCts.CancelAfter(EngineDefaults.DiscoveryTimeout);

                var buildingContext = new Building.TestBuildingContext(isForExecution, Filter: null);
                var tests = await _testBuilderPipeline.BuildTestsFromMetadataAsync(
                    metadataToInclude,
                    buildingContext,
                    filterCts.Token).ConfigureAwait(false);

                var testsList = tests.ToList();

                foreach (var test in testsList)
                {
                    _cachedTests.Enqueue(test);
                    _dependencyResolver.RegisterTest(test);
                }

                allTests.AddRange(testsList);
            }

            _dependencyResolver.BatchResolveDependencies(allTests);
            _testBuilderPipeline.PopulateAllDependencies(allTests);

            return allTests;
        }
#if NET
        catch (Exception ex)
        {
            TUnitActivitySource.RecordException(discoveryActivity, ex);
            throw;
        }
#else
        catch
        {
            throw;
        }
#endif
        finally
        {
#if NET
            discoveryActivity?.SetTag(TUnitActivitySource.TagTestCount, allTests.Count);
            TUnitActivitySource.StopActivity(discoveryActivity);
#endif
        }
    }

    private async IAsyncEnumerable<AbstractExecutableTest> DiscoverTestsStreamAsync(
        string testSessionId,
        Building.TestBuildingContext buildingContext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(EngineDefaults.DiscoveryTimeout);

        var tests = await _testBuilderPipeline.BuildTestsAsync(testSessionId, buildingContext, metadataFilter: null, cts.Token).ConfigureAwait(false);

        foreach (var test in tests)
        {
            _dependencyResolver.RegisterTest(test);
            _cachedTests.Enqueue(test);
            yield return test;
        }
    }

    // NOTE: This streaming path never calls _testFilterService.RegisterTestsAsync, so yielded
    // tests have NOT had argument registration (shared-object creation + reference counting).
    // It currently has no callers; if it is ever wired into an execution path, tests must be
    // registered with isForExecution: true before they run, or property injection and shared
    // fixture disposal will silently break.
    #if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Generic test instantiation requires MakeGenericType")]
    #endif
    public async IAsyncEnumerable<AbstractExecutableTest> DiscoverTestsFullyStreamingAsync(
        string testSessionId,
        ITestExecutionFilter? filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _testExecutor.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

        var contextProvider = _testExecutor.GetContextProvider();

        var allTests = await DiscoverAndResolveTestsAsync(
            testSessionId, filter: null, isForExecution: false, cancellationToken).ConfigureAwait(false);

        allTests = await ApplyAfterDiscoveryHooksAsync(contextProvider, allTests, cancellationToken).ConfigureAwait(false);

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

        // Kahn's algorithm: build in-degree map and reverse-dependency map for O(V+E) topological sort
        var inDegree = new Dictionary<string, int>(dependentTests.Count);
        var reverseDeps = new Dictionary<string, List<AbstractExecutableTest>>();

        foreach (var test in dependentTests)
        {
            var pendingCount = 0;
            foreach (var dep in test.Dependencies)
            {
                if (!yieldedTests.Contains(dep.Test.TestId))
                {
                    pendingCount++;
                }

                // Build reverse map: when dep.Test is yielded, this test's in-degree decreases
                var depId = dep.Test.TestId;
                if (!reverseDeps.TryGetValue(depId, out var dependents))
                {
                    dependents = [];
                    reverseDeps[depId] = dependents;
                }
                dependents.Add(test);
            }

            inDegree[test.TestId] = pendingCount;
        }

        // Seed queue with dependent tests whose dependencies are all already yielded
        var readyQueue = new Queue<AbstractExecutableTest>();
        foreach (var test in dependentTests)
        {
            if (inDegree[test.TestId] == 0)
            {
                readyQueue.Enqueue(test);
            }
        }

        var yieldedCount = 0;
        while (readyQueue.Count > 0)
        {
            var test = readyQueue.Dequeue();
            yieldedCount++;

            if (_testFilterService.MatchesTest(filter, test))
            {
                yield return test;
            }

            yieldedTests.Add(test.TestId);

            // Decrement in-degree for all tests that depend on this one
            if (reverseDeps.TryGetValue(test.TestId, out var dependentsOfTest))
            {
                foreach (var dependent in dependentsOfTest)
                {
                    var newDegree = --inDegree[dependent.TestId];
                    if (newDegree == 0)
                    {
                        readyQueue.Enqueue(dependent);
                    }
                }
            }
        }

        // Cycle-break fallback: if some tests remain, their dependency graph has a cycle.
        // Yield them without respecting order — CircularDependencyDetector will report
        // the cycle with a proper error during scheduling.
        if (yieldedCount < dependentTests.Count)
        {
            foreach (var test in dependentTests)
            {
                if (!yieldedTests.Contains(test.TestId))
                {
                    if (_testFilterService.MatchesTest(filter, test))
                    {
                        yield return test;
                    }
                }
            }
        }
    }



    private Task InvokePostResolutionEventsInParallelAsync(List<AbstractExecutableTest> allTests)
    {
        return Utilities.ParallelMap.ForEachParallelAsync(
            allTests,
            test => _testBuilderPipeline.InvokePostResolutionEventsAsync(test),
            Environment.ProcessorCount);
    }

    public IEnumerable<TestContext> GetCachedTestContexts()
    {
        var excluded = _hookExcludedTests;
        var list = new List<TestContext>(_cachedTests.Count);
        foreach (var test in _cachedTests)
        {
            if (excluded is null || !excluded.Contains(test.Context))
            {
                list.Add(test.Context);
            }
        }
        return list;
    }
}
