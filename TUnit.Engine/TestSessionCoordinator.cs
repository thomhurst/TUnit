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
        await ExpandDeferredPlaceholdersAsync(testList, cancellationToken);

        InitializeEventReceivers(testList, cancellationToken);

        try
        {
            await PrepareTestOrchestrator(testList, cancellationToken);
            await ExecuteTestsCore(testList, cancellationToken);
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
    /// produced by enumerating its data source. The placeholder itself is reported (Passed, or Failed if the
    /// data source throws) so the IDE node it occupies gets a result; the children are added to the list and
    /// scheduled like any other test, nested under the placeholder via their ParentTestId.
    /// </summary>
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT/trimmed scenarios")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection mode is not used in AOT scenarios")]
#endif
    private async Task ExpandDeferredPlaceholdersAsync(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
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
            return;
        }

        foreach (var placeholder in placeholders)
        {
            // Remove the placeholder so it is never scheduled as a real test (its Create/Invoke throw).
            testList.Remove(placeholder);

            placeholder.StartTime = DateTimeOffset.UtcNow;
            await _messageBus.InProgress(placeholder.Context);

            try
            {
                var children = await _deferredTestExpander.ExpandAsync(placeholder, cancellationToken);
                testList.AddRange(children);

                placeholder.EndTime = DateTimeOffset.UtcNow;
                placeholder.SetResult(TestState.Passed);
                await _messageBus.Passed(placeholder.Context, placeholder.StartTime.GetValueOrDefault());
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Failed to expand deferred test '{placeholder.TestId}': {ex}");
                placeholder.EndTime = DateTimeOffset.UtcNow;
                placeholder.SetResult(TestState.Failed, ex);
                await _messageBus.Failed(placeholder.Context, ex, placeholder.StartTime.GetValueOrDefault());
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
