using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Configuration;
using TUnit.Engine.Models;
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

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Scoped attribute filtering uses Type.GetInterfaces and reflection")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Generic test instantiation requires MakeGenericType")]
    #endif
    public async Task<TestDiscoveryResult> DiscoverTests(string testSessionId, ITestExecutionFilter? filter, CancellationToken cancellationToken, bool isForExecution)
    {
        await _testExecutor.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

        var contextProvider = _testExecutor.GetContextProvider();
        contextProvider.BeforeTestDiscoveryContext.RestoreExecutionContext();

        // Step 1: Stream lightweight TestNodeInfo objects
        var allTestNodeInfos = await _testBuilderPipeline.StreamTestNodeInfoAsync(testSessionId, cancellationToken).ConfigureAwait(false);

        // Step 2: Determine which tests to build based on filter and execution mode
        List<TestNodeInfo> testNodeInfosToBuild;

        if (!isForExecution)
        {
            // Discovery mode (IDE): Build all tests
            testNodeInfosToBuild = allTestNodeInfos;
        }
        else
        {
            // Execution mode: Apply filtering
            var filteredTestNodeInfos = new List<TestNodeInfo>();
            var explicitTestNodeInfos = new List<TestNodeInfo>();

            foreach (var testNodeInfo in allTestNodeInfos)
            {
                if (_testFilterService.MatchesTestNode(filter, testNodeInfo))
                {
                    if (_testFilterService.IsExplicitTest(testNodeInfo))
                    {
                        explicitTestNodeInfos.Add(testNodeInfo);
                    }
                    else
                    {
                        filteredTestNodeInfos.Add(testNodeInfo);
                    }
                }
            }

            // Handle explicit tests logic
            if (filteredTestNodeInfos.Count > 0 && explicitTestNodeInfos.Count > 0)
            {
                // If we have both, exclude explicit tests (filter wasn't targeting them)
                testNodeInfosToBuild = filteredTestNodeInfos;
            }
            else if (explicitTestNodeInfos.Count > 0)
            {
                // Only explicit tests matched - include them
                testNodeInfosToBuild = explicitTestNodeInfos;
            }
            else
            {
                testNodeInfosToBuild = filteredTestNodeInfos;
            }
        }

        // Step 3: Find all tests we need to build (filtered tests + their dependencies recursively)
        var testNodeInfosToInclude = new HashSet<TestNodeInfo>();
        var queue = new Queue<TestNodeInfo>();

        // Start with filtered tests
        foreach (var testNodeInfo in testNodeInfosToBuild)
        {
            if (testNodeInfosToInclude.Add(testNodeInfo))
            {
                queue.Enqueue(testNodeInfo);
            }
        }

        // Add all dependencies recursively
        while (queue.Count > 0)
        {
            var testNodeInfo = queue.Dequeue();

            // Check metadata for dependencies
            foreach (var dependency in testNodeInfo.Metadata.Dependencies)
            {
                // Find all tests that match this dependency
                foreach (var candidateNodeInfo in allTestNodeInfos)
                {
                    if (dependency.Matches(candidateNodeInfo.Metadata, testNodeInfo.Metadata))
                    {
                        if (testNodeInfosToInclude.Add(candidateNodeInfo))
                        {
                            queue.Enqueue(candidateNodeInfo);
                        }
                    }
                }
            }
        }

        // Step 4: Build full tests only for filtered tests and their dependencies
        var filteredTests = new List<AbstractExecutableTest>();

        foreach (var testNodeInfo in testNodeInfosToInclude)
        {
            var test = await _testBuilderPipeline.BuildTestFromNodeInfoAsync(testNodeInfo, cancellationToken).ConfigureAwait(false);
            filteredTests.Add(test);
            _dependencyResolver.RegisterTest(test);
            _cachedTests.Add(test);
        }

        // Step 5: Resolve dependencies for built tests
        foreach (var test in filteredTests.Where(t => t.Metadata.Dependencies.Length > 0))
        {
            _dependencyResolver.TryResolveDependencies(test);
        }

        // Step 6: For hooks, we need ALL test contexts (not just filtered ones)
        // Build lightweight tests for remaining tests (those that were filtered out)
        // This ensures hooks see all discovered tests
        List<AbstractExecutableTest> allTestsForContext;

        if (isForExecution && testNodeInfosToInclude.Count < allTestNodeInfos.Count)
        {
            // Some tests were filtered out - build them too for context (but don't register them)
            allTestsForContext = new List<AbstractExecutableTest>(filteredTests);
            var filteredIds = new HashSet<string>(filteredTests.Select(t => t.TestId));

            foreach (var testNodeInfo in allTestNodeInfos)
            {
                if (!filteredIds.Contains(testNodeInfo.TestId))
                {
                    try
                    {
                        var test = await _testBuilderPipeline.BuildTestFromNodeInfoAsync(testNodeInfo, cancellationToken).ConfigureAwait(false);
                        allTestsForContext.Add(test);
                        _cachedTests.Add(test);
                    }
                    catch (Exception)
                    {
                        // Skip tests that fail to build
                        continue;
                    }
                }
            }
        }
        else
        {
            // No filtering applied or discovery mode - all tests are included
            allTestsForContext = filteredTests;
        }

        contextProvider.TestDiscoveryContext.AddTests(allTestsForContext.Select(static t => t.Context));

        await _testExecutor.ExecuteAfterTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);

        contextProvider.TestDiscoveryContext.RestoreExecutionContext();

        // Register only the filtered tests (not the ones built just for hooks)
        await _testFilterService.RegisterTestsAsync(filteredTests).ConfigureAwait(false);

        // Capture the final execution context after discovery
        var finalContext = ExecutionContext.Capture();
        return new TestDiscoveryResult(filteredTests, finalContext);
    }

    public IEnumerable<TestContext> GetCachedTestContexts()
    {
        return _cachedTests.Select(static t => t.Context);
    }
}
