using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Configuration;
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

        var filterTypes = TestFilterTypeExtractor.ExtractTypesFromFilter(filter);
        var estimatedTestCount = 1000;
        var allTests = ObjectPools.RentTestList(estimatedTestCount);
        var dependentTestIndices = ObjectPools.RentIntList();

        await foreach (var test in DiscoverTestsStreamAsync(testSessionId, filterTypes, cancellationToken).ConfigureAwait(false))
        {
            var index = allTests.Count;
            allTests.Add(test);
            
            _dependencyResolver.RegisterTest(test);

            if (test.Metadata.Dependencies.Length > 0)
            {
                dependentTestIndices.Add(index);
            }
        }

        foreach (var index in dependentTestIndices)
        {
            _dependencyResolver.TryResolveDependencies(allTests[index]);
        }

        var tests = allTests;

        // For discovery requests (IDE test explorers), return all tests including explicit ones
        // For execution requests, apply filtering to exclude explicit tests unless explicitly targeted
        List<AbstractExecutableTest> filteredTests;
        
        if (!isForExecution)
        {
            filteredTests = tests;
        }
        else
        {
            var filtered = _testFilterService.FilterTests(filter, tests);
            
#if NETSTANDARD2_0
            var testsToInclude = new HashSet<AbstractExecutableTest>();
            var queue = new Queue<AbstractExecutableTest>();
#else
            var testsToInclude = new HashSet<AbstractExecutableTest>(filtered.Count * 2);
            var queue = new Queue<AbstractExecutableTest>(filtered.Count);
#endif
            
            foreach (var test in filtered)
            {
                testsToInclude.Add(test);
                queue.Enqueue(test);
            }

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

            filteredTests = new List<AbstractExecutableTest>(testsToInclude.Count);
            filteredTests.AddRange(testsToInclude);
        }

        var contexts = new TestContext[allTests.Count];
        for (var i = 0; i < allTests.Count; i++)
        {
            contexts[i] = allTests[i].Context;
        }
        contextProvider.TestDiscoveryContext.AddTests(contexts);

        await _testExecutor.ExecuteAfterTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

        contextProvider.TestDiscoveryContext.RestoreExecutionContext();

        // Register the filtered tests to invoke ITestRegisteredEventReceiver
        await _testFilterService.RegisterTestsAsync(filteredTests).ConfigureAwait(false);

        ObjectPools.ReturnIntList(dependentTestIndices);
        if (allTests != filteredTests && allTests != tests)
        {
            ObjectPools.ReturnTestList(allTests);
        }
        
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
            // Configure from environment if needed
            DiscoveryConfiguration.ConfigureFromEnvironment();
            cts.CancelAfter(DiscoveryConfiguration.DiscoveryTimeout);
        }

        var tests = await _testBuilderPipeline.BuildTestsStreamingAsync(testSessionId, filterTypes, cancellationToken).ConfigureAwait(false);

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
    public async IAsyncEnumerable<AbstractExecutableTest> DiscoverTestsFullyStreamingAsync(
        string testSessionId,
        ITestExecutionFilter? filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _testExecutor.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

        // Extract types from filter for optimized discovery
        var filterTypes = TestFilterTypeExtractor.ExtractTypesFromFilter(filter);

        // Use pooled list for collection
        var estimatedCount = 1000;
        var allTests = ObjectPools.RentTestList(estimatedCount);
        await foreach (var test in DiscoverTestsStreamAsync(testSessionId, filterTypes, cancellationToken).ConfigureAwait(false))
        {
            allTests.Add(test);
        }

        // Resolve dependencies for all tests
        for (var i = 0; i < allTests.Count; i++)
        {
            _dependencyResolver.TryResolveDependencies(allTests[i]);
        }

        // Use pooled lists for indices
        var independentIndices = ObjectPools.RentIntList(allTests.Count);
        var dependentIndices = ObjectPools.RentIntList(allTests.Count);

        for (var i = 0; i < allTests.Count; i++)
        {
            if (allTests[i].Dependencies.Length == 0)
            {
                independentIndices.Add(i);
            }
            else
            {
                dependentIndices.Add(i);
            }
        }

        // Yield independent tests first using indices
        foreach (var index in independentIndices)
        {
            var test = allTests[index];
            if (_testFilterService.MatchesTest(filter, test))
            {
                yield return test;
            }
        }

        // Use pooled collections
        var yieldedTests = ObjectPools.RentStringHashSet(independentIndices.Count);
        foreach (var index in independentIndices)
        {
            yieldedTests.Add(allTests[index].TestId);
        }
        
        var remainingIndices = ObjectPools.RentIntList(dependentIndices.Count);
        remainingIndices.AddRange(dependentIndices);

        while (remainingIndices.Count > 0)
        {
            var readyIndices = ObjectPools.RentIntList();

            for (var i = remainingIndices.Count - 1; i >= 0; i--) // Iterate backwards for safe removal
            {
                var testIndex = remainingIndices[i];
                var test = allTests[testIndex];
                
                // Check if all dependencies have been yielded
                var allDependenciesYielded = true;
                foreach (var dep in test.Dependencies)
                {
                    if (!yieldedTests.Contains(dep.Test.TestId))
                    {
                        allDependenciesYielded = false;
                        break;
                    }
                }

                if (allDependenciesYielded)
                {
                    readyIndices.Add(testIndex);
                    remainingIndices.RemoveAt(i);
                }
            }

            // If no tests are ready, we have a circular dependency
            if (readyIndices.Count == 0 && remainingIndices.Count > 0)
            {
                // Yield remaining tests anyway to avoid hanging
                foreach (var index in remainingIndices)
                {
                    var test = allTests[index];
                    if (_testFilterService.MatchesTest(filter, test))
                    {
                        yield return test;
                    }
                }
                break;
            }

            // Yield ready tests
            foreach (var index in readyIndices)
            {
                var test = allTests[index];
                if (_testFilterService.MatchesTest(filter, test))
                {
                    yield return test;
                }
                yieldedTests.Add(test.TestId);
            }
            
            ObjectPools.ReturnIntList(readyIndices);
        }
        
        // Clean up pooled objects
        ObjectPools.ReturnIntList(independentIndices);
        ObjectPools.ReturnIntList(dependentIndices);
        ObjectPools.ReturnIntList(remainingIndices);
        ObjectPools.ReturnStringHashSet(yieldedTests);
        ObjectPools.ReturnTestList(allTests);
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
