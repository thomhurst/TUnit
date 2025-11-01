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

        // DEPENDENCY FIX: Disable early filtering optimization to ensure dependencies are discovered
        // When a test depends on another test, we must discover ALL tests so that dependencies
        // can be resolved. The early filtering optimization in TestBuilder would skip building
        // dependency tests that don't match the filter, breaking dependency resolution.
        // Trade-off: This means all tests are built even when filtering, which has performance cost.
        // TODO: Optimize by implementing metadata-level dependency analysis to determine
        // which tests to build before construction (requires pipeline architecture changes).
        var buildingContext = new Building.TestBuildingContext(isForExecution, Filter: null);

        var allTests = new List<AbstractExecutableTest>();

        await foreach (var test in DiscoverTestsStreamAsync(testSessionId, buildingContext, cancellationToken).ConfigureAwait(false))
        {
            allTests.Add(test);
        }

        // Resolve dependencies for all tests
        foreach (var test in allTests)
        {
            _dependencyResolver.RegisterTest(test);
        }

        foreach (var test in allTests.Where(t => t.Metadata.Dependencies.Length > 0))
        {
            _dependencyResolver.TryResolveDependencies(test);
        }

        // For discovery requests (IDE test explorers), return all tests
        // For execution requests, apply filtering to exclude explicit tests unless explicitly targeted
        var filteredTests = isForExecution ? _testFilterService.FilterTests(filter, allTests) : allTests;

        // Add back any dependencies that were filtered out
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

    /// <summary>
    /// Filters test metadata to find tests that could match the filter.
    /// This is a lightweight operation that doesn't require building tests.
    /// Uses the same logic as TestBuilder.CouldTestMatchFilter but inlined to avoid dependencies.
    /// </summary>
    private List<TestMetadata> FilterMetadata(IEnumerable<TestMetadata> allMetadata, ITestExecutionFilter filter)
    {
        var result = new List<TestMetadata>();

        foreach (var metadata in allMetadata)
        {
            if (CouldMetadataMatchFilter(filter, metadata))
            {
                result.Add(metadata);
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if metadata could potentially match the filter without building the test.
    /// Conservative check - returns true unless we can definitively rule out the test.
    /// </summary>
#pragma warning disable TPEXP
    private bool CouldMetadataMatchFilter(ITestExecutionFilter filter, TestMetadata metadata)
    {
        return filter switch
        {
            null => true,
            NopFilter => true,
            TreeNodeFilter treeFilter => CouldMatchTreeNodeFilter(treeFilter, metadata),
            TestNodeUidListFilter uidFilter => CouldMatchUidFilter(uidFilter, metadata),
            _ => true // Unknown filter type - be conservative
        };
    }

    private bool CouldMatchUidFilter(TestNodeUidListFilter filter, TestMetadata metadata)
    {
        var classMetadata = metadata.MethodMetadata.Class;
        var namespaceName = classMetadata.Namespace ?? "";
        var className = metadata.TestClassType.Name;
        var methodName = metadata.TestMethodName;

        foreach (var uid in filter.TestNodeUids)
        {
            var uidValue = uid.Value;
            if (uidValue.Contains(namespaceName) &&
                uidValue.Contains(className) &&
                uidValue.Contains(methodName))
            {
                return true;
            }
        }

        return false;
    }

    private bool CouldMatchTreeNodeFilter(TreeNodeFilter filter, TestMetadata metadata)
    {
        var filterString = filter.Filter;

        if (string.IsNullOrEmpty(filterString))
        {
            return true;
        }

        // Strip property conditions for path-only matching
        TreeNodeFilter pathOnlyFilter;
        if (filterString.Contains('['))
        {
            var strippedFilterString = System.Text.RegularExpressions.Regex.Replace(
                filterString, @"\[([^\]]*)\]", "");
            pathOnlyFilter = CreateTreeNodeFilterViaReflection(strippedFilterString);
        }
        else
        {
            pathOnlyFilter = filter;
        }

        var path = BuildPathFromMetadata(metadata);
        var emptyPropertyBag = new PropertyBag();
        return pathOnlyFilter.MatchesFilter(path, emptyPropertyBag);
    }

    private static TreeNodeFilter CreateTreeNodeFilterViaReflection(string filterString)
    {
        var constructor = typeof(TreeNodeFilter).GetConstructors(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];
        return (TreeNodeFilter)constructor.Invoke([filterString]);
    }

    private static string BuildPathFromMetadata(TestMetadata metadata)
    {
        var classMetadata = metadata.MethodMetadata.Class;
        var assemblyName = classMetadata.Assembly.Name ?? metadata.TestClassType.Assembly.GetName().Name ?? "*";
        var namespaceName = classMetadata.Namespace ?? "*";
        var className = classMetadata.Name;
        var methodName = metadata.TestMethodName;

        return $"/{assemblyName}/{namespaceName}/{className}/{methodName}";
    }
#pragma warning restore TPEXP

    /// <summary>
    /// Builds tests from a specific subset of metadata.
    /// This allows us to build only the tests we need (filtered + dependencies).
    /// </summary>
    private async IAsyncEnumerable<AbstractExecutableTest> BuildTestsFromMetadataSubset(
        HashSet<TestMetadata> metadataSubset,
        Building.TestBuildingContext buildingContext,
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Use the pipeline's existing streaming build functionality
        // but only for the metadata subset we care about
        var testsStream = await _testBuilderPipeline.BuildTestsStreamingAsync(testSessionId, buildingContext, cancellationToken).ConfigureAwait(false);

        foreach (var test in testsStream)
        {
            // Only yield tests whose metadata is in our subset
            if (metadataSubset.Contains(test.Metadata))
            {
                _dependencyResolver.RegisterTest(test);
                _cachedTests.Add(test);
                yield return test;
            }
        }
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
