using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Building;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Unified test discovery service that uses the new pipeline architecture
/// </summary>
public sealed class TestDiscoveryServiceV2 : IDataProducer
{
    private const int DiscoveryTimeoutSeconds = 30;
    private readonly UnifiedTestBuilderPipeline _testBuilderPipeline;

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
    public async Task<IEnumerable<ExecutableTest>> DiscoverTests()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(DiscoveryTimeoutSeconds));

        try
        {
            return await DiscoverTestsWithTimeout(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Test discovery timed out after {DiscoveryTimeoutSeconds} seconds. " +
                "This may indicate an issue with data sources or excessive test generation.");
        }
    }

    private async Task<IEnumerable<ExecutableTest>> DiscoverTestsWithTimeout(CancellationToken cancellationToken)
    {
        // Use the pipeline to build all tests
        var allTests = new List<ExecutableTest>();

        await foreach (var test in BuildTestsAsync(cancellationToken))
        {
            allTests.Add(test);
        }

        // No longer using TestRegistry - tests are managed directly

        // Resolve dependencies between tests
        ResolveDependencies(allTests);

        return allTests;
    }

    private async IAsyncEnumerable<ExecutableTest> BuildTestsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var executableTests = await _testBuilderPipeline.BuildTestsAsync();

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
                
                // Populate the TestContext.Dependencies list for user access
                test.Context.Dependencies.Clear();
                foreach (var dep in test.Dependencies)
                {
                    test.Context.Dependencies.Add(dep.DisplayName);
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
}
