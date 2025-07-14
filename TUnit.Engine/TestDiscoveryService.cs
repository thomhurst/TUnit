using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Building;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Unified test discovery service that uses the new pipeline architecture
/// </summary>
public sealed class TestDiscoveryService : IDataProducer, IStreamingTestDiscovery
{
    private const int DiscoveryTimeoutSeconds = 60;
    private readonly UnifiedTestBuilderPipeline _testBuilderPipeline;
    private readonly ConcurrentBag<ExecutableTest> _cachedTests = new();
    private readonly TestDependencyResolver _dependencyResolver = new();

    public string Uid => "TUnit";
    public string Version => "2.0.0";
    public string DisplayName => "TUnit Test Discovery";
    public string Description => "TUnit Unified Test Discovery Service";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public TestDiscoveryService(UnifiedTestBuilderPipeline testBuilderPipeline)
    {
        _testBuilderPipeline = testBuilderPipeline ?? throw new ArgumentNullException(nameof(testBuilderPipeline));
    }

    /// <summary>
    /// Discovers all tests using the unified pipeline
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> DiscoverTests(string testSessionId)
    {
        // Use streaming internally but collect results for backward compatibility
        var tests = new List<ExecutableTest>();
        await foreach (var test in DiscoverTestsStreamAsync(testSessionId))
        {
            tests.Add(test);
        }
        return tests;
    }

    /// <summary>
    /// Discovers tests as a stream, enabling parallel discovery and execution
    /// </summary>
    public async IAsyncEnumerable<ExecutableTest> DiscoverTestsStreamAsync(
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (!Debugger.IsAttached)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(DiscoveryTimeoutSeconds));
        }

        await foreach (var test in BuildTestsAsync(testSessionId, cts.Token))
        {
            _dependencyResolver.RegisterTest(test);

            // Try to resolve dependencies immediately
            if (!_dependencyResolver.TryResolveDependencies(test) && test.Metadata.Dependencies.Length > 0)
            {
                // Mark as waiting if dependencies not ready
                test.State = TestState.WaitingForDependencies;
            }

            // Cache for backward compatibility
            _cachedTests.Add(test);

            yield return test;
        }
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



    /// <summary>
    /// Gets all cached test contexts for use by TestFinder
    /// </summary>
    public IEnumerable<TestContext> GetCachedTestContexts()
    {
        return _cachedTests.Select(t => t.Context);
    }
}
