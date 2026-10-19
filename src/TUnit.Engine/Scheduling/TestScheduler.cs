using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Configuration;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;
using TUnit.Core.Settings;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.Engine.Scheduling;

internal sealed class TestScheduler : ITestScheduler
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestGroupingService _groupingService;
    private readonly ITUnitMessageBus _messageBus;
    private readonly TestStateManager _testStateManager;
    private readonly TestRunner _testRunner;
    private readonly CircularDependencyDetector _circularDependencyDetector;
    private readonly IConstraintKeyScheduler _constraintKeyScheduler;
    private readonly HookExecutor _hookExecutor;
    private readonly AfterHookPairTracker _afterHookPairTracker;
    private readonly StaticPropertyHandler _staticPropertyHandler;
    private readonly IDynamicTestQueue _dynamicTestQueue;
    private readonly NotInParallelLock _notInParallelLock;
    private readonly MaxParallelismLimiter _maxParallelismLimiter;

    // Dynamic tests the queue processor has scheduled, so a cancelled run can report the ones
    // that never started. Null until the first dynamic test arrives.
    private List<AbstractExecutableTest>? _dynamicTests;

    public TestScheduler(
        TUnitFrameworkLogger logger,
        ITestGroupingService groupingService,
        ITUnitMessageBus messageBus,
        MaxParallelismLimiter maxParallelismLimiter,
        TestStateManager testStateManager,
        TestRunner testRunner,
        CircularDependencyDetector circularDependencyDetector,
        IConstraintKeyScheduler constraintKeyScheduler,
        HookExecutor hookExecutor,
        AfterHookPairTracker afterHookPairTracker,
        StaticPropertyHandler staticPropertyHandler,
        IDynamicTestQueue dynamicTestQueue,
        NotInParallelLock notInParallelLock)
    {
        _logger = logger;
        _groupingService = groupingService;
        _messageBus = messageBus;
        _testStateManager = testStateManager;
        _testRunner = testRunner;
        _circularDependencyDetector = circularDependencyDetector;
        _constraintKeyScheduler = constraintKeyScheduler;
        _hookExecutor = hookExecutor;
        _afterHookPairTracker = afterHookPairTracker;
        _staticPropertyHandler = staticPropertyHandler;
        _dynamicTestQueue = dynamicTestQueue;
        _notInParallelLock = notInParallelLock;

        _maxParallelismLimiter = maxParallelismLimiter;
    }

    #if NET
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    public async Task<bool> ScheduleAndExecuteAsync(
        List<AbstractExecutableTest> testList,
        CancellationToken cancellationToken)
    {
        if (testList == null)
        {
            throw new ArgumentNullException(nameof(testList));
        }

        if (testList.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found").ConfigureAwait(false);
            return true;
        }

        if (_logger.IsDebugEnabled)
            await _logger.LogDebugAsync($"Scheduling execution of {testList.Count} tests").ConfigureAwait(false);

        var hasDependencies = HasDependencies(testList);
        var executableTests = hasDependencies
            ? await RemoveCircularDependenciesAsync(testList, cancellationToken).ConfigureAwait(false)
            : testList;

        if (executableTests.Count == 0)
        {
            await _logger.LogDebugAsync("No executable tests found after removing circular dependencies").ConfigureAwait(false);
            return true;
        }

        // Covers every phase that runs user code (static property initializers, tests,
        // session hooks) so blocking tests do not delay timers in other tests.
        using var starvationMonitor = ThreadPoolStarvationMonitor.Start(_maxParallelismLimiter.MaxParallelism, _logger);

        // Initialize static properties before tests run
        await _staticPropertyHandler.InitializeStaticPropertiesAsync(cancellationToken).ConfigureAwait(false);

        // Track static properties for disposal at session end
        _staticPropertyHandler.TrackStaticProperties();

        // Group tests by their parallel constraints
        var groupedTests = await _groupingService.GroupTestsByConstraintsAsync(executableTests).ConfigureAwait(false);

        if (hasDependencies)
        {
            MarkDependencyRelatedTestsForExecutionDedup(executableTests);
        }

        // Suites with no global [NotInParallel] tests skip the runtime exclusion
        // lock entirely. Once enabled, the flag is monotonic — dynamic batches
        // that introduce NIP later (see ExecuteDynamicBatchAsync) keep it on.
        MarkGlobalNotInParallelTests(groupedTests);

        bool sessionHooksSucceeded;
        try
        {
            // Execute tests according to their grouping
            await ExecuteGroupedTestsAsync(groupedTests, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Before the session hooks, so every selected test has a result by the time the
            // session ends.
            if (cancellationToken.IsCancellationRequested)
            {
                await ReportTestsNotStartedAsync(executableTests).ConfigureAwait(false);
            }

            // Also on the fail-fast path, where cancelled tests make the phases throw: the
            // cancellation callback only starts the session hooks, so await them (and the
            // static property disposal that follows them) here, before the caller disposes
            // shared fixtures the hooks may still be using.
            sessionHooksSucceeded = await RunAfterTestSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        return sessionHooksSucceeded;
    }

    // A cancelled run (fail-fast, timeout or abort) stops tests before they start: the scheduler
    // stops handing out slots and the global [NotInParallel] phase never runs. Without a result
    // those tests would vanish from the run's totals, so give each one a result now (#7119). Tests
    // that started report their own result, so only those still waiting are reported here.
    private async ValueTask ReportTestsNotStartedAsync(List<AbstractExecutableTest> executableTests)
    {
        // Fail-fast is the user's request to stop after a failure, so its unstarted tests are
        // skipped; any other cancellation aborted the run, so those tests are cancelled.
        var skipReason = _testRunner.IsFailFastTriggered
            ? "Test was not run because --fail-fast stopped the test run after a failure"
            : null;

        try
        {
            foreach (var test in executableTests)
            {
                await ReportIfNotStartedAsync(test, skipReason).ConfigureAwait(false);
            }

            if (_dynamicTests is { } dynamicTests)
            {
                foreach (var test in dynamicTests)
                {
                    await ReportIfNotStartedAsync(test, skipReason).ConfigureAwait(false);
                }
            }

            // Dynamic tests still queued when the processor stopped on the cancelled token.
            while (_dynamicTestQueue.TryDequeue(out var test))
            {
                if (test != null)
                {
                    await ReportIfNotStartedAsync(test, skipReason).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            // Best effort: the platform can stop accepting messages once its own token is
            // cancelled, and that must not replace the run's outcome.
            await _logger.LogDebugAsync($"Could not report tests that were not started: {ex}").ConfigureAwait(false);
        }
    }

    private async ValueTask ReportIfNotStartedAsync(AbstractExecutableTest test, string? skipReason)
    {
        if (test.State is not (TestState.NotStarted or TestState.WaitingForDependencies or TestState.Queued))
        {
            return;
        }

        if (skipReason is not null)
        {
            _testStateManager.MarkSkipped(test, skipReason);
            await _messageBus.Skipped(test.Context, skipReason).ConfigureAwait(false);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        test.StartTime ??= now;
        test.EndTime = now;
        test.SetResult(TestState.Cancelled);
        TestSessionContext.Current?.MarkFailure();
        await _messageBus.Cancelled(test.Context, test.StartTime.GetValueOrDefault()).ConfigureAwait(false);
    }

    private async Task<bool> RunAfterTestSessionAsync(CancellationToken cancellationToken)
    {
        var sessionHookExceptions = await _afterHookPairTracker.GetOrCreateAfterTestSessionTask(
            () => _hookExecutor.ExecuteAfterTestSessionHooksAsync(cancellationToken)).ConfigureAwait(false) ?? [];

        await _staticPropertyHandler.DisposeStaticPropertiesAsync(sessionHookExceptions).ConfigureAwait(false);

        if (sessionHookExceptions.Count > 0)
        {
            foreach (var ex in sessionHookExceptions)
            {
                await _logger.LogErrorAsync($"Error executing After(TestSession) hook: {ex}").ConfigureAwait(false);
            }
            return false;
        }

        return true;
    }

    private static bool HasDependencies(List<AbstractExecutableTest> tests)
    {
        foreach (var test in tests)
        {
            if (test.Dependencies.Length != 0)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<List<AbstractExecutableTest>> RemoveCircularDependenciesAsync(
        List<AbstractExecutableTest> tests,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var circularDependencies = _circularDependencyDetector.DetectCircularDependencies(tests, cancellationToken);
        HashSet<AbstractExecutableTest>? testsInCircularDependencies = null;

        foreach (var (_, dependencyChain) in circularDependencies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            testsInCircularDependencies ??= [];

            var simpleNames = new List<string>(dependencyChain.Count);
            foreach (var test in dependencyChain)
            {
                cancellationToken.ThrowIfCancellationRequested();
                simpleNames.Add($"{test.Metadata.TestClassType.Name}.{test.Metadata.TestMethodName}");
            }

            var exception = new CircularDependencyException(
                $"DependsOn Conflict: {string.Join(" > ", simpleNames)}");

            foreach (var test in dependencyChain)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (testsInCircularDependencies.Add(test))
                {
                    _testStateManager.MarkCircularDependencyFailed(test, exception);
                    TestSessionContext.Current?.MarkFailure();
                    await _messageBus.Failed(test.Context, exception, DateTimeOffset.UtcNow).ConfigureAwait(false);
                }
            }
        }

        if (testsInCircularDependencies is null)
        {
            return tests;
        }

        var executableTests = new List<AbstractExecutableTest>(tests.Count - testsInCircularDependencies.Count);
        foreach (var test in tests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!testsInCircularDependencies.Contains(test))
            {
                executableTests.Add(test);
            }
        }

        return executableTests;
    }

    #if NET
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteGroupedTestsAsync(
        GroupedTests groupedTests,
        CancellationToken cancellationToken)
    {
        // Start dynamic test queue processing in background
        var dynamicTestProcessingTask = ProcessDynamicTestQueueAsync(cancellationToken);

        try
        {
            await ExecuteAllPhasesAsync(groupedTests, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // On the fail-fast path the phases throw while dynamic tests may still be running.
            // Wait for them before the caller runs session teardown; the phase failure wins.
            _dynamicTestQueue.Complete();
            try
            {
                await dynamicTestProcessingTask.ConfigureAwait(false);
            }
            catch
            {
                // Swallowed: rethrowing the phase exception below.
            }

            throw;
        }

        // Mark the queue as complete and wait for remaining dynamic tests to finish
        _dynamicTestQueue.Complete();
        await dynamicTestProcessingTask.ConfigureAwait(false);
    }

    #if NET
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteAllPhasesAsync(GroupedTests groupedTests, CancellationToken cancellationToken)
    {
        // Unconstrained Parallel and KeyedNotInParallel buckets share no state — keyed tests
        // only block other tests sharing a key (per docs/parallelism.md). Running them
        // sequentially makes a `[Test] T1` always run before `[Test, NotInParallel("k")] T2`
        // which is observable as "Test1 never overlaps Test2" (discussion #5700). Run them
        // concurrently instead. ParallelGroups still serialize (cross-group exclusion).
        // Global NotInParallel runs last in declared [NotInParallel(Order)] order; the
        // "alone" semantic is enforced at runtime by NotInParallelLock, not by this phase.
        Task? parallelPhase = null;
        if (groupedTests.Parallel.Length > 0)
        {
            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Starting {groupedTests.Parallel.Length} parallel tests").ConfigureAwait(false);
            parallelPhase = ExecuteTestsAsync(groupedTests.Parallel, cancellationToken);
        }

        Task? keyedPhase = null;
        if (groupedTests.KeyedNotInParallel.Length > 0)
        {
            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Starting {groupedTests.KeyedNotInParallel.Length} keyed NotInParallel tests").ConfigureAwait(false);
            keyedPhase = _constraintKeyScheduler.ExecuteTestsWithConstraintsAsync(groupedTests.KeyedNotInParallel, cancellationToken).AsTask();
        }

        await RunPhasesConcurrentlyAsync(parallelPhase, keyedPhase).ConfigureAwait(false);

        // Each ParallelGroup runs to completion before the next — tests are bucketed
        // into a group precisely because they must not overlap with tests in another group.
        foreach (var group in groupedTests.ParallelGroups)
        {
            var totalCount = 0;
            foreach (var list in group.Value.Values) totalCount += list.Count;
            var orderedTestsArray = new AbstractExecutableTest[totalCount];
            var idx = 0;
            foreach (var list in group.Value.Values)
            {
                foreach (var t in list)
                {
                    orderedTestsArray[idx++] = t;
                }
            }

            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Starting parallel group '{group.Key}' with {orderedTestsArray.Length} orders").ConfigureAwait(false);
            await ExecuteTestsAsync(orderedTestsArray, cancellationToken).ConfigureAwait(false);
        }

        foreach (var kvp in groupedTests.ConstrainedParallelGroups)
        {
            var constrainedTests = kvp.Value;
            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Starting constrained parallel group '{kvp.Key}' with {constrainedTests.UnconstrainedTests.Length} unconstrained and {constrainedTests.KeyedTests.Length} keyed tests").ConfigureAwait(false);

            var unconstrainedPhase = constrainedTests.UnconstrainedTests.Length > 0
                ? ExecuteTestsAsync(constrainedTests.UnconstrainedTests, cancellationToken)
                : null;
            var groupKeyedPhase = constrainedTests.KeyedTests.Length > 0
                ? _constraintKeyScheduler.ExecuteTestsWithConstraintsAsync(constrainedTests.KeyedTests, cancellationToken).AsTask()
                : null;
            await RunPhasesConcurrentlyAsync(unconstrainedPhase, groupKeyedPhase).ConfigureAwait(false);
        }

        if (groupedTests.NotInParallel.Length > 0)
        {
            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Starting {groupedTests.NotInParallel.Length} global NotInParallel tests").ConfigureAwait(false);
            await ExecuteSequentiallyAsync(groupedTests.NotInParallel, cancellationToken).ConfigureAwait(false);
        }
    }

    #if NET
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ProcessDynamicTestQueueAsync(CancellationToken cancellationToken)
    {
        var dynamicTests = new List<AbstractExecutableTest>();

        // Use async signaling instead of polling to eliminate IOCP overhead
        while (await WaitForDynamicTestsAsync(cancellationToken).ConfigureAwait(false))
        {
            // Dequeue all currently available tests (batch processing)
            while (_dynamicTestQueue.TryDequeue(out var test))
            {
                if (test != null)
                {
                    dynamicTests.Add(test);
                }
            }

            // Execute the batch of dynamic tests if any were found
            if (dynamicTests.Count > 0)
            {
                TrackDynamicTests(dynamicTests);

                if (_logger.IsTraceEnabled)
                    await _logger.LogTraceAsync($"Executing {dynamicTests.Count} dynamic test(s)").ConfigureAwait(false);

                await ExecuteDynamicBatchAsync(dynamicTests, cancellationToken).ConfigureAwait(false);
                dynamicTests.Clear();
            }
        }

        // Cancelled (fail-fast or run abort): the queued tests would only be cancelled too, and
        // the phases would throw on the cancelled token, so stop here.
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // Process any remaining tests after queue completion
        while (_dynamicTestQueue.TryDequeue(out var test))
        {
            if (test != null)
            {
                dynamicTests.Add(test);
            }
        }

        if (dynamicTests.Count > 0)
        {
            TrackDynamicTests(dynamicTests);

            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Executing {dynamicTests.Count} remaining dynamic test(s)").ConfigureAwait(false);

            await ExecuteDynamicBatchAsync(dynamicTests, cancellationToken).ConfigureAwait(false);
        }
    }

    // Only the queue processor writes this, and the cancelled-run report reads it after the
    // processor has finished, so it needs no locking.
    private void TrackDynamicTests(List<AbstractExecutableTest> dynamicTests)
    {
        (_dynamicTests ??= []).AddRange(dynamicTests);
    }

    // Ends the wait loop instead of faulting when the token is cancelled. The phases can finish
    // without throwing after fail-fast fires (the failing test was the last to run), and
    // ExecuteGroupedTestsAsync then awaits this processor on its normal path, so a cancelled
    // wait would surface as the scheduler's exception.
    private async ValueTask<bool> WaitForDynamicTestsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _dynamicTestQueue.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    #if NET
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteDynamicBatchAsync(List<AbstractExecutableTest> dynamicTests, CancellationToken cancellationToken)
    {
        // Route through the same phase pipeline as the main test set so dynamically-added
        // tests with [NotInParallel(key)], [ParallelGroup(...)] etc. honour their constraints
        // instead of being silently dropped.
        var groupedDynamicTests = await _groupingService.GroupTestsByConstraintsAsync(dynamicTests.ToArray()).ConfigureAwait(false);
        MarkDependencyRelatedTestsForExecutionDedup(dynamicTests);
        MarkGlobalNotInParallelTests(groupedDynamicTests);
        await ExecuteAllPhasesAsync(groupedDynamicTests, cancellationToken).ConfigureAwait(false);
    }

    internal static void MarkDependencyRelatedTestsForExecutionDedup(IEnumerable<AbstractExecutableTest> tests)
    {
        HashSet<AbstractExecutableTest>? visitedDependencyTargets = null;
        Stack<AbstractExecutableTest>? pendingDependencyTargets = null;

        foreach (var test in tests)
        {
            if (test.Dependencies.Length == 0)
            {
                continue;
            }

            test.RequiresExecutionDedup = true;

            visitedDependencyTargets ??= [];
            // Shared across roots so a common dependency target is marked once even
            // when multiple scheduled tests reference it.
            pendingDependencyTargets ??= [];

            foreach (var dependency in test.Dependencies)
            {
                pendingDependencyTargets.Push(dependency.Test);
            }

            while (pendingDependencyTargets.Count > 0)
            {
                var dependencyTarget = pendingDependencyTargets.Pop();

                if (!visitedDependencyTargets.Add(dependencyTarget))
                {
                    continue;
                }

                if (dependencyTarget.State == TestState.Failed &&
                    dependencyTarget.Result?.Exception is CircularDependencyException)
                {
                    dependencyTarget.ExecutionTask ??= Task.CompletedTask;
                    continue;
                }

                dependencyTarget.RequiresExecutionDedup = true;

                foreach (var nestedDependency in dependencyTarget.Dependencies)
                {
                    pendingDependencyTargets.Push(nestedDependency.Test);
                }
            }
        }
    }

    private void MarkGlobalNotInParallelTests(GroupedTests grouped)
    {
        if (grouped.NotInParallel.Length == 0)
        {
            return;
        }

        _notInParallelLock.Enable();

        for (var i = 0; i < grouped.NotInParallel.Length; i++)
        {
            grouped.NotInParallel[i].RequiresGlobalNotInParallelLock = true;
        }
    }

#if NET
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private Task ExecuteTestsAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        // All paths run through the shared limiter so the DOP cap is unified in
        // GetMaxParallelism. "Unlimited" is resolved to the default cap
        // (ProcessorCount * 4) there rather than being a separate code path.
        return ExecuteWithGlobalLimitAsync(tests, _maxParallelismLimiter.Semaphore, cancellationToken);
    }

#if NET
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteSequentiallyAsync(
        AbstractExecutableTest[] tests,
        CancellationToken cancellationToken)
    {
        foreach (var test in tests)
        {
            // Stop on a cancelled run instead of walking every remaining test into a cancelled
            // lock wait. The tests left behind are reported once the run winds down.
            cancellationToken.ThrowIfCancellationRequested();
            await ExecuteScheduledTestAsync(test, cancellationToken).ConfigureAwait(false);
        }
    }

#if NET
    private Task ExecuteWithGlobalLimitAsync(
        AbstractExecutableTest[] tests,
        SemaphoreSlim globalSemaphore,
        CancellationToken cancellationToken)
    {
        // MaxDegreeOfParallelism bounds this call's workers; the shared semaphore bounds the
        // session, because the keyed phase and dynamic batches run concurrently with this one.
        // An uncontended WaitAsync completes synchronously without allocating.
        return Parallel.ForEachAsync(
            tests,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxParallelismLimiter.MaxParallelism,
                CancellationToken = cancellationToken
            },
            async (test, ct) =>
            {
                await globalSemaphore.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    await ExecuteScheduledTestAsync(test, ct).ConfigureAwait(false);
                }
                finally
                {
                    globalSemaphore.Release();
                }
            }
        );
    }
#else
    private async Task ExecuteWithGlobalLimitAsync(
        AbstractExecutableTest[] tests,
        SemaphoreSlim globalSemaphore,
        CancellationToken cancellationToken)
    {
        // Fallback for netstandard2.0: Manual bounded concurrency using existing semaphore
        var tasks = new Task[tests.Length];
        for (var i = 0; i < tests.Length; i++)
        {
            var test = tests[i];
            tasks[i] = Task.Run(async () =>
            {
                await globalSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await ExecuteScheduledTestAsync(test, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    globalSemaphore.Release();
                }
            }, CancellationToken.None);
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
#endif

    private async ValueTask ExecuteScheduledTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        if (!test.RequiresExecutionDedup)
        {
            await _testRunner.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Dependency recursion is the re-entrant path. Only those tests reuse the
        // scheduler task slot; independent tests may also use it for constraint wrappers.
        test.ExecutionTask ??= _testRunner.ExecuteTestAsync(test, cancellationToken).AsTask();
        await test.ExecutionTask.ConfigureAwait(false);
    }

    // The phases are already started and carry the token themselves. When fail-fast stops them,
    // the cancellation propagates as-is: the failing test already has its own result, and
    // re-throwing its exception here made the framework report it a second time as an
    // "Unhandled exception" node (#7119).
    private static Task RunPhasesConcurrentlyAsync(Task? a, Task? b)
    {
        if (a is null) return b ?? Task.CompletedTask;
        if (b is null) return a;
        return Task.WhenAll(a, b);
    }

    internal static int GetMaxParallelism(ILogger logger, ICommandLineOptions commandLineOptions)
    {
        // "Unlimited" (caller passed 0) resolves to the same cap as the default path.
        // Parallel.ForEachAsync with MaxDegreeOfParallelism = -1 is truly unbounded —
        // with thousands of tests this saturates IOCP threads, so we always apply a cap.
        var defaultLimit = MaxParallelismLimiter.DefaultMaxParallelism;

        // Check command line argument first (highest priority)
        if (commandLineOptions.TryGetOptionArgumentList(
                MaximumParallelTestsCommandProvider.MaximumParallelTests,
                out var args) && args.Length > 0 && int.TryParse(args[0], out var maxParallelTests))
        {
            if (maxParallelTests == 0)
            {
                // 0 historically meant "unlimited"; we now treat it the same as the
                // default cap so the unlimited path is not a regression against the
                // default path.
                logger.LogDebug($"Maximum parallel tests: unlimited requested from command line, capped to default {defaultLimit} to avoid IOCP saturation");
                return defaultLimit;
            }

            if (maxParallelTests > 0)
            {
                logger.LogDebug($"Maximum parallel tests limit set to {maxParallelTests} (from command line)");
                return maxParallelTests;
            }
        }

        // Check environment variable (second priority)
        if (Environment.GetEnvironmentVariable(EnvironmentConstants.MaxParallelTests) is string envVar
            && int.TryParse(envVar, out var envLimit))
        {
            if (envLimit == 0)
            {
                logger.LogDebug($"Maximum parallel tests: unlimited requested from TUNIT_MAX_PARALLEL_TESTS, capped to default {defaultLimit} to avoid IOCP saturation");
                return defaultLimit;
            }

            if (envLimit > 0)
            {
                logger.LogDebug($"Maximum parallel tests limit set to {envLimit} (from TUNIT_MAX_PARALLEL_TESTS environment variable)");
                return envLimit;
            }
        }

        // Check TUnitSettings (third priority — code-level project defaults)
        if (TUnitSettings.Default.Parallelism.MaximumParallelTests is { } codeLimit)
        {
            if (codeLimit == 0)
            {
                logger.LogDebug($"Maximum parallel tests: unlimited requested from TUnitSettings, capped to default {defaultLimit} to avoid IOCP saturation");
                return defaultLimit;
            }

            logger.LogDebug($"Maximum parallel tests limit set to {codeLimit} (from TUnitSettings)");
            return codeLimit;
        }

        // Default: 4x CPU cores (empirically optimized for async/IO-bound workloads)
        // Users can override via --maximum-parallel-tests or TUNIT_MAX_PARALLEL_TESTS
        logger.LogDebug($"Maximum parallel tests limit defaulting to {defaultLimit} ({Environment.ProcessorCount} processors * 4)");
        return defaultLimit;
    }
}
