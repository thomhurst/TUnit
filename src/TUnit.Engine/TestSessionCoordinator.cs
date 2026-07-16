using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Services;
using TUnit.Core.Tracking;
using TUnit.Engine.Framework;
using TUnit.Engine.Logging;
using TUnit.Engine.Scheduling;
using TUnit.Engine.Services;
using ITestExecutor = TUnit.Engine.Interfaces.ITestExecutor;

namespace TUnit.Engine;

internal sealed class TestSessionCoordinator : ITestExecutor, IDisposable, IAsyncDisposable
{
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestScheduler _testScheduler;
    private readonly TUnitServiceProvider _serviceProvider;
    private readonly IContextProvider _contextProvider;
    private readonly TestLifecycleCoordinator _lifecycleCoordinator;
    private readonly ITUnitMessageBus _messageBus;
    private readonly IStaticPropertyInitializer _staticPropertyInitializer;
    private readonly ObjectTracker _objectTracker;
    private readonly DeferredTestExpander _deferredTestExpander;

    public TestSessionCoordinator(EventReceiverOrchestrator eventReceiverOrchestrator,
        TUnitFrameworkLogger logger,
        ITestScheduler testScheduler,
        TUnitServiceProvider serviceProvider,
        IContextProvider contextProvider,
        TestLifecycleCoordinator lifecycleCoordinator,
        ITUnitMessageBus messageBus,
        IStaticPropertyInitializer staticPropertyInitializer,
        ObjectTracker objectTracker,
        DeferredTestExpander deferredTestExpander)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _contextProvider = contextProvider;
        _lifecycleCoordinator = lifecycleCoordinator;
        _messageBus = messageBus;
        _testScheduler = testScheduler;
        _staticPropertyInitializer = staticPropertyInitializer;
        _objectTracker = objectTracker;
        _deferredTestExpander = deferredTestExpander;
    }

    public async Task ExecuteTests(
        IEnumerable<AbstractExecutableTest> tests,
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        var testList = tests.ToList();

        // Expand any deferred-enumeration placeholders into their real cases before counting/scheduling,
        // so the children flow through the normal pipeline (correct hooks + lifecycle counting).
        var expandedPlaceholders = await ExpandDeferredPlaceholdersAsync(testList, cancellationToken);

        InitializeEventReceivers(testList, cancellationToken);

        try
        {
            await PrepareTestOrchestrator(testList, cancellationToken);
            await ExecuteTestsCore(testList, cancellationToken);

            // Children have now run: resolve each placeholder container to the aggregate of its cases so the
            // IDE node the user ran gets a result (rather than "not run") that reflects its children.
            await ReportDeferredPlaceholderResultsAsync(expandedPlaceholders);
        }
        finally
        {
            // Dispose anything still ref-counted (e.g. consumers were cancelled or filtered out)
            // and reset the static shared-instance caches so a subsequent run request in the same
            // process (IDE server mode) creates fresh fixtures instead of reusing disposed ones.
            // Runs after After(TestSession) hooks and static property disposal (both inside
            // ExecuteTestsCore via the scheduler), preserving existing disposal ordering.
            var sweepExceptions = await _objectTracker.DisposeAndClearStaticTrackingAsync();

            if (sweepExceptions is { Count: > 0 })
            {
                foreach (var exception in sweepExceptions)
                {
                    await _logger.LogErrorAsync($"Error disposing tracked object at session end: {exception}");
                }
            }

            TestDataContainer.Reset();

            foreach (var artifact in _contextProvider.TestSessionContext.Artifacts)
            {
                await _messageBus.SessionArtifact(artifact);
            }
        }
    }

    private void InitializeEventReceivers(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        _eventReceiverOrchestrator.InitializeTestCounts(testList);
    }

    /// <summary>
    /// Replaces every <see cref="DeferredEnumerationExecutableTest"/> in the list with the real test cases
    /// produced by enumerating its data source. The placeholder is reported as a running container now (so the
    /// IDE node the user ran shows as in-progress rather than "not run") and resolved to the aggregate of its
    /// children later by <see cref="ReportDeferredPlaceholderResultsAsync"/>; the children are added to the
    /// list, scheduled like any other test, and nested under the placeholder via their ParentTestId. If the
    /// expansion itself throws, the placeholder is reported failed immediately. (Per-row data errors surface
    /// as their own failed child via the standard data-generation-error path.) The returned list pairs each
    /// successfully-expanded placeholder with its children so its final result can be reported post-run.
    /// </summary>
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT/trimmed scenarios")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection mode is not used in AOT scenarios")]
#endif
    private async Task<List<(AbstractExecutableTest Placeholder, IReadOnlyList<AbstractExecutableTest> Children)>> ExpandDeferredPlaceholdersAsync(
        List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        List<DeferredEnumerationExecutableTest>? placeholders = null;
        foreach (var test in testList)
        {
            if (test is DeferredEnumerationExecutableTest placeholder)
            {
                (placeholders ??= []).Add(placeholder);
            }
        }

        if (placeholders is null)
        {
            return [];
        }

        // Drop all placeholders in a single pass so none are scheduled as real tests (their Create/Invoke
        // throw); their children are added back below.
        testList.RemoveAll(static t => t is DeferredEnumerationExecutableTest);

        var expanded = new List<(AbstractExecutableTest, IReadOnlyList<AbstractExecutableTest>)>(placeholders.Count);

        foreach (var placeholder in placeholders)
        {
            placeholder.StartTime = DateTimeOffset.UtcNow;
            placeholder.State = TestState.Running;
            await _messageBus.InProgress(placeholder.Context);

            try
            {
                var children = await _deferredTestExpander.ExpandAsync(placeholder, cancellationToken);
                testList.AddRange(children);
                expanded.Add((placeholder, children));
            }
            catch (Exception ex)
            {
                // Expansion itself failed (as opposed to a per-row data error, which becomes a failed
                // child). Surface it on the placeholder node so the failure is visible.
                await _logger.LogErrorAsync($"Failed to expand deferred test '{placeholder.TestId}': {ex}");
                placeholder.EndTime = DateTimeOffset.UtcNow;
                placeholder.SetResult(TestState.Failed, ex);
                await _messageBus.Failed(placeholder.Context, ex, placeholder.StartTime.GetValueOrDefault());
            }
        }

        return expanded;
    }

    /// <summary>
    /// Reports the final result for each deferred placeholder once its children have executed: the placeholder
    /// is a container whose outcome is the aggregate of its cases — failed if any case failed, skipped if every
    /// case was skipped, otherwise passed. This resolves the IDE node the user ran without masking child
    /// failures (which a fixed "passed" would).
    /// </summary>
    private async Task ReportDeferredPlaceholderResultsAsync(
        List<(AbstractExecutableTest Placeholder, IReadOnlyList<AbstractExecutableTest> Children)> expandedPlaceholders)
    {
        foreach (var (placeholder, children) in expandedPlaceholders)
        {
            placeholder.EndTime = DateTimeOffset.UtcNow;

            var failedCount = 0;
            var skippedCount = 0;
            foreach (var child in children)
            {
                switch (child.State)
                {
                    case TestState.Failed or TestState.Timeout or TestState.Cancelled:
                        failedCount++;
                        break;
                    case TestState.Skipped:
                        skippedCount++;
                        break;
                }
            }

            if (failedCount > 0)
            {
                var exception = new Exception($"{failedCount} of {children.Count} deferred test case(s) failed.");
                placeholder.SetResult(TestState.Failed, exception);
                await _messageBus.Failed(placeholder.Context, exception, placeholder.StartTime.GetValueOrDefault());
            }
            else if (children.Count > 0 && skippedCount == children.Count)
            {
                placeholder.State = TestState.Skipped;
                await _messageBus.Skipped(placeholder.Context, "All deferred test cases were skipped");
            }
            else
            {
                placeholder.SetResult(TestState.Passed);
                await _messageBus.Passed(placeholder.Context, placeholder.StartTime.GetValueOrDefault());
            }
        }
    }

    private async Task PrepareTestOrchestrator(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        // Register all tests upfront so orchestrator knows total counts per class/assembly for lifecycle management
        _lifecycleCoordinator.RegisterTests(testList);

        await _staticPropertyInitializer.InitializeAsync(cancellationToken);
    }


    #if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test scheduler uses mode-specific services that handle reflection properly")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test scheduler uses mode-specific services that handle dynamic code properly")]
    #endif
    private async Task ExecuteTestsCore(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        // Combine cancellation tokens from multiple sources:
        // - cancellationToken: Per-request cancellation from test platform
        // - FailFastCancellationSource: Triggered when fail-fast is enabled and a test fails
        // - CancellationToken: Engine-level graceful shutdown (e.g., Ctrl+C)
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _serviceProvider.FailFastCancellationSource.Token,
            _serviceProvider.CancellationToken.Token);

        // Schedule and execute tests (batch approach to preserve ExecutionContext)
        var success = await _testScheduler.ScheduleAndExecuteAsync(testList, linkedCts.Token);

        if (!success)
        {
            _serviceProvider.SessionFailed = true;
        }
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return default(ValueTask);
        }

        _disposed = true;

        return default(ValueTask);
    }
}
