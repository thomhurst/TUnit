using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Building;

namespace TUnit.Engine;

/// <summary>
/// Unified test discovery service that uses the new pipeline architecture
/// </summary>
public sealed class TestDiscoveryServiceV2 : IDataProducer
{
    private const int DiscoveryTimeoutSeconds = 60;
    private readonly UnifiedTestBuilderPipeline _testBuilderPipeline;
    private readonly ConcurrentBag<ExecutableTest> _cachedTests = new();
    private bool _discoveryCompleted;

    public string Uid => "TUnit";
    public string Version => "2.0.0";
    public string DisplayName => "TUnit Test Discovery";
    public string Description => "TUnit Unified Test Discovery Service";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public TestDiscoveryServiceV2(UnifiedTestBuilderPipeline testBuilderPipeline)
    {
        _testBuilderPipeline = testBuilderPipeline ?? throw new ArgumentNullException(nameof(testBuilderPipeline));
    }

    /// <summary>
    /// Discovers all tests using the unified pipeline
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> DiscoverTests(string testSessionId)
    {
        using var cts = new CancellationTokenSource();

        if (!Debugger.IsAttached)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(DiscoveryTimeoutSeconds));
        }

        try
        {
            return await DiscoverTestsWithTimeout(testSessionId, cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Test discovery timed out after {DiscoveryTimeoutSeconds} seconds. " +
                "This may indicate an issue with data sources or excessive test generation.");
        }
    }

    private async Task<IEnumerable<ExecutableTest>> DiscoverTestsWithTimeout(string testSessionId, CancellationToken cancellationToken)
    {
        // If discovery has already been completed, return cached tests
        if (_discoveryCompleted)
        {
            return _cachedTests.ToList();
        }

        // Use the pipeline to build all tests
        var allTests = new List<ExecutableTest>();

        await foreach (var test in BuildTestsAsync(testSessionId, cancellationToken))
        {
            allTests.Add(test);
            _cachedTests.Add(test);
        }

        // No longer using TestRegistry - tests are managed directly

        // Resolve dependencies between tests
        ResolveDependencies(allTests);

        _discoveryCompleted = true;
        return allTests;
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

    private void ResolveDependencies(List<ExecutableTest> allTests)
    {
        foreach (var test in allTests)
        {
            try
            {
                var dependencies = new List<ExecutableTest>();

                // Handle new TestDependency model
                foreach (var dependency in test.Metadata.Dependencies)
                {
                    var matchingTests = allTests.Where(t => dependency.Matches(t.Metadata, test.Metadata)).ToList();

                    if (matchingTests.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Test '{test.DisplayName}' depends on {dependency} which was not found.");
                    }

                    dependencies.AddRange(matchingTests);
                }

                // Remove duplicates and ensure we don't depend on ourselves
                test.Dependencies = dependencies
                    .Distinct()
                    .Where(d => d.TestId != test.TestId)
                    .ToArray();

                // Populate the TestContext.Dependencies list with all dependencies (including transitive)
                test.Context.Dependencies.Clear();
                var allDependencies = GetAllDependencies(test, new HashSet<string>());
                foreach (var dep in allDependencies)
                {
                    test.Context.Dependencies.Add(dep.Context.TestDetails);
                }
            }
            catch (Exception ex)
            {
                // Mark test as failed due to dependency resolution error
                test.State = TestState.Failed;
                test.Result = new TestResult
                {
                    Status = Status.Failed,
                    Start = DateTimeOffset.UtcNow,
                    End = DateTimeOffset.UtcNow,
                    Duration = TimeSpan.Zero,
                    Exception = ex,
                    ComputerName = Environment.MachineName
                };

                // Clear dependencies so test won't be scheduled
                test.Dependencies = [];
            }
        }
    }

    private List<ExecutableTest> GetAllDependencies(ExecutableTest test, HashSet<string> visited)
    {
        var result = new List<ExecutableTest>();

        // Add this test's ID to visited to prevent cycles
        visited.Add(test.TestId);

        foreach (var dependency in test.Dependencies)
        {
            if (!visited.Contains(dependency.TestId))
            {
                // Add the direct dependency
                result.Add(dependency);

                // Recursively add transitive dependencies
                result.AddRange(GetAllDependencies(dependency, visited));
            }
        }

        // Remove duplicates while preserving order
        return result.Distinct().ToList();
    }

    /// <summary>
    /// Gets all cached test contexts for use by TestFinder
    /// </summary>
    public IEnumerable<TestContext> GetCachedTestContexts()
    {
        return _cachedTests.Select(t => t.Context);
    }
}
