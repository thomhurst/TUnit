using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;
#if NET
using System.Diagnostics;
#endif

namespace TUnit.Engine;

/// <summary>
/// Simple orchestrator that composes focused services to manage test execution flow.
/// Follows Single Responsibility Principle and SOLID principles.
/// </summary>
internal class TestExecutor
{
    private static readonly ConcurrentDictionary<Type, bool> ClassHookPresenceCache = new();
    private static readonly ConcurrentDictionary<Type, bool> TestHookPresenceCache = new();

    private readonly HookExecutor _hookExecutor;
    private readonly TestLifecycleCoordinator _lifecycleCoordinator;
    private readonly BeforeHookTaskCache _beforeHookTaskCache;
    private readonly AfterHookPairTracker _afterHookPairTracker;
    private readonly IContextProvider _contextProvider;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;

    // Cached hook-factory delegates so the per-test before-hook awaits don't allocate a fresh closure
    // each time (these run on the hot path and the factory is only invoked on the first cache miss).
    private readonly Func<CancellationToken, ValueTask> _beforeTestSessionHookFactory;
    private readonly Func<Assembly, CancellationToken, ValueTask> _beforeAssemblyHookFactory;
    private readonly Func<Assembly, ValueTask<List<Exception>>> _cancelledAfterAssemblyHookFactory;
    private readonly AfterClassExecutor _cancelledAfterClassHookFactory;
#if NET
    private readonly Func<Assembly, ValueTask<List<Exception>>> _finishAssemblyActivityFactory;
    private readonly AfterClassExecutor _finishClassActivityFactory;
#endif

    [UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "Class cleanup delegates receive only test-class types annotated at the execution boundary.")]
    [UnconditionalSuppressMessage("Trimming", "IL2111",
        Justification = "The annotated class cleanup method is captured as a delegate, not accessed through reflection.")]
    public TestExecutor(
        HookExecutor hookExecutor,
        TestLifecycleCoordinator lifecycleCoordinator,
        BeforeHookTaskCache beforeHookTaskCache,
        AfterHookPairTracker afterHookPairTracker,
        IContextProvider contextProvider,
        EventReceiverOrchestrator eventReceiverOrchestrator)
    {
        _hookExecutor = hookExecutor;
        _lifecycleCoordinator = lifecycleCoordinator;
        _beforeHookTaskCache = beforeHookTaskCache;
        _afterHookPairTracker = afterHookPairTracker;
        _contextProvider = contextProvider;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;

        _beforeTestSessionHookFactory = ct => _hookExecutor.ExecuteBeforeTestSessionHooksAsync(ct);
        _beforeAssemblyHookFactory = (assembly, ct) => _hookExecutor.ExecuteBeforeAssemblyHooksAsync(assembly, ct);
        _cancelledAfterAssemblyHookFactory = assembly => _hookExecutor.ExecuteAfterAssemblyHooksAsync(assembly, CancellationToken.None);
        _cancelledAfterClassHookFactory = testClass => _hookExecutor.ExecuteAfterClassHooksAsync(testClass, CancellationToken.None);
#if NET
        _finishAssemblyActivityFactory = _hookExecutor.FinishAssemblyActivityAsync;
        _finishClassActivityFactory = _hookExecutor.FinishClassActivityAsync;
#endif
    }


    /// <summary>
    /// Ensures that Before(TestSession) hooks have been executed.
    /// This is called before creating test instances to ensure resources are available.
    /// Registers the corresponding After(TestSession) hook to run on cancellation.
    /// </summary>
    public async ValueTask EnsureTestSessionHooksExecutedAsync(CancellationToken cancellationToken)
    {
        if (!HasTestSessionHooks())
        {
            return;
        }

        // Get or create and cache Before hooks - these run only once
        await _beforeHookTaskCache.GetOrCreateBeforeTestSessionTask(
            _beforeTestSessionHookFactory,
            cancellationToken).ConfigureAwait(false);

        // Register After Session hook to run on cancellation (guarantees cleanup)
        _afterHookPairTracker.RegisterAfterTestSessionHook(
            cancellationToken,
            () => _hookExecutor.ExecuteAfterTestSessionHooksAsync(CancellationToken.None));
    }

    /// <summary>
    /// Ensures Before(TestSession), Before(Assembly) and Before(Class) hooks (including their
    /// BeforeEvery counterparts) have executed before the test class instance is constructed, so the
    /// documented lifecycle contract (hooks run before instantiation) holds — see issue #6192.
    /// The hook tasks are cached in <see cref="BeforeHookTaskCache"/>, so the matching awaits later in
    /// <see cref="ExecuteAsync"/> are no-ops. Only the pure cached getters are invoked here; the
    /// After-hook pair registration stays single-sourced in ExecuteAsync to avoid double-registration.
    /// </summary>
    public async ValueTask EnsureClassAndAssemblyHooksExecutedAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        var testClass = test.Metadata.TestClassType;

        if (HasTestSessionHooks())
        {
            await _beforeHookTaskCache.GetOrCreateBeforeTestSessionTask(
                _beforeTestSessionHookFactory,
                cancellationToken).ConfigureAwait(false);
        }

        // Flow AsyncLocals captured by BeforeTestSession into the BeforeAssembly hook, and likewise
        // BeforeAssembly into BeforeClass. This mirrors the RestoreExecutionContext chain in
        // ExecuteAsync (the assembly/class hook tasks are cached, so when ExecuteAsync re-awaits them
        // it re-applies the same captured contexts).
        test.Context.ClassContext.AssemblyContext.TestSessionContext.RestoreExecutionContext();

        var hasAssemblyHooks = HasAssemblyHooks(testClass.Assembly);
        if (hasAssemblyHooks)
        {
            await _beforeHookTaskCache.GetOrCreateBeforeAssemblyTask(
                testClass.Assembly,
                _beforeAssemblyHookFactory,
                cancellationToken).ConfigureAwait(false);
        }
#if NET
        else
        {
            _hookExecutor.TryStartAssemblyActivity(testClass.Assembly);
        }
#endif

        test.Context.ClassContext.AssemblyContext.RestoreExecutionContext();

        var hasClassHooks = HasClassHooks(testClass);
        if (hasClassHooks)
        {
            await _beforeHookTaskCache.GetOrCreateBeforeClassTask(testClass, _hookExecutor, cancellationToken).ConfigureAwait(false);
        }
#if NET
        else
        {
            _hookExecutor.TryStartClassActivity(testClass);
        }
#endif

        // Note: the caller (TestCoordinator) restores ClassContext.RestoreExecutionContext() right
        // before constructing the instance so AsyncLocals captured by BeforeAssembly/BeforeClass flow
        // into the constructor. Restoring here wouldn't persist — the ambient ExecutionContext is
        // reset when this async method returns to the caller.
    }

    /// <summary>
    /// Creates a test executor delegate that wraps the provided executor with hook orchestration.
    /// Uses focused services that follow SRP to manage lifecycle and execution.
    /// </summary>
    public async ValueTask ExecuteAsync(AbstractExecutableTest executableTest, TestInitializer testInitializer, CancellationToken cancellationToken, TimeSpan? testTimeout = null)
    {
        executableTest.Context.InitializeTestCancellation(cancellationToken);

        var testClass = executableTest.Metadata.TestClassType;
        var testAssembly = testClass.Assembly;
        var hasSessionHooks = HasTestSessionHooks();
        var hasAssemblyHooks = HasAssemblyHooks(testAssembly);
        var hasClassHooks = HasClassHooks(testClass);
        var hasTestHooks = HasTestHooks(testClass);

        Exception? capturedException = null;
        Exception? hookException = null;

        try
        {
            if (hasSessionHooks)
            {
                await EnsureTestSessionHooksExecutedAsync(cancellationToken).ConfigureAwait(false);
            }

            await _eventReceiverOrchestrator.InvokeFirstTestInSessionEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext.AssemblyContext.TestSessionContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.AssemblyContext.TestSessionContext.RestoreExecutionContext();

            if (hasAssemblyHooks)
            {
                await _beforeHookTaskCache.GetOrCreateBeforeAssemblyTask(
                    testAssembly,
                    _beforeAssemblyHookFactory,
                    cancellationToken).ConfigureAwait(false);
            }
#if NET
            else
            {
                _hookExecutor.TryStartAssemblyActivity(testAssembly);
            }
#endif

            var cancelledAfterAssemblyFactory = ResolveAssemblyCleanup(
                testAssembly,
                hasAssemblyHooks,
                _cancelledAfterAssemblyHookFactory);

            if (cancelledAfterAssemblyFactory is not null)
            {
                // Register lifecycle cleanup on cancellation.
                _afterHookPairTracker.RegisterAfterAssemblyHook(
                    testAssembly,
                    cancellationToken,
                    cancelledAfterAssemblyFactory);
            }

            await _eventReceiverOrchestrator.InvokeFirstTestInAssemblyEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext.AssemblyContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.AssemblyContext.RestoreExecutionContext();

            if (hasClassHooks)
            {
                await _beforeHookTaskCache.GetOrCreateBeforeClassTask(testClass, _hookExecutor, cancellationToken).ConfigureAwait(false);
            }
#if NET
            else
            {
                _hookExecutor.TryStartClassActivity(testClass);
            }
#endif

            var cancelledAfterClassFactory = ResolveClassCleanup(
                testClass,
                hasClassHooks,
                _cancelledAfterClassHookFactory);

            if (cancelledAfterClassFactory is not null)
            {
                // Register lifecycle cleanup on cancellation.
                _afterHookPairTracker.RegisterAfterClassHook(
                    testClass,
                    cancellationToken,
                    cancelledAfterClassFactory);
            }

            await _eventReceiverOrchestrator.InvokeFirstTestInClassEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.RestoreExecutionContext();

#if NET
            // Each test case starts its own trace so each test gets a unique W3C TraceId
            // for natural OTEL distributed tracing correlation. We must clear Activity.Current
            // because StartActivity with parentContext: default falls back to Activity.Current
            // when it's non-null, which would make all tests in a class share the class TraceId.
            // Class/session lifecycle spans stay on the separate TUnit.Lifecycle source.
            if (TUnitActivitySource.Source.HasListeners())
            {
                var testDetails = executableTest.Context.Metadata.TestDetails;

                // Clear ambient activity so StartActivity creates a root (new TraceId).
                // Safe: Activity.Current is AsyncLocal, so this only affects this async context.
                Activity.Current = null;

                executableTest.Context.Activity = TUnitActivitySource.StartActivity(
                    TUnitActivitySource.SpanTestCase,
                    ActivityKind.Internal,
                    parentContext: default,
                    [
                        new(TUnitActivitySource.TagTestCaseName, testDetails.TestName),
                        new(TUnitActivitySource.TagTestSuiteName, testDetails.ClassType.Name),
                        new(TUnitActivitySource.TagTestClass, testDetails.ClassType.FullName),
                        new(TUnitActivitySource.TagClassNamespace, testDetails.ClassType.Namespace),
                        new(TUnitActivitySource.TagTestMethod, testDetails.MethodName),
                        new(TUnitActivitySource.TagAssemblyName, testAssembly.GetName().Name),
                        new(TUnitActivitySource.TagSessionId, executableTest.Context.ClassContext.AssemblyContext.TestSessionContext.Id),
                        new(TUnitActivitySource.TagTestId, executableTest.Context.Id),
                        new(TUnitActivitySource.TagTestNodeUid, testDetails.TestId),
                        new(TUnitActivitySource.TagTestCategories, testDetails.Categories.ToArray())
                    ]);

                executableTest.Context.Activity?.SetBaggage(TUnitActivitySource.TagTestId, executableTest.Context.Id);

                // Register for OTLP receiver cross-process log correlation
                if (executableTest.Context.Activity is { } testActivity)
                {
                    TraceRegistry.Register(
                        testActivity.TraceId.ToString(),
                        testDetails.TestId,
                        executableTest.Context.Id);
                }
            }
#endif

            // The session/assembly/class RestoreExecutionContext calls above replay AsyncLocals
            // captured while the FIRST test in that scope ran its hooks — including that test's
            // TestContext.Current. Re-point Current at this test before initializing objects so
            // IAsyncInitializer implementations observe the correct test context.
            TestContext.Current = executableTest.Context;

            // Initialize test objects (IAsyncInitializer) AFTER BeforeClass hooks
            // and after the test case activity starts. Per-test objects are traced
            // under the test case; shared objects under session/assembly/class.
            await testInitializer.InitializeTestObjectsAsync(executableTest, cancellationToken).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            // Early stage test start receivers run before instance-level hooks
            await _eventReceiverOrchestrator.InvokeTestStartEventReceiversAsync(executableTest.Context, cancellationToken, EventReceiverStage.Early).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            if (hasTestHooks)
            {
                await _hookExecutor.ExecuteBeforeTestHooksAsync(executableTest, cancellationToken).ConfigureAwait(false);
            }

            // Late stage test start receivers run after instance-level hooks (default behavior)
            await _eventReceiverOrchestrator.InvokeTestStartEventReceiversAsync(executableTest.Context, cancellationToken, EventReceiverStage.Late).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            // Only the test body is subject to the [Timeout] — hooks and data source
            // initialization run outside the timeout scope (fixes #4772)
#if NET
            Activity? testBodyActivity = null;
            if (TUnitActivitySource.Source.HasListeners())
            {
                // Restore Activity.Current to the test case activity so the test body
                // becomes a natural child (with Activity.Parent set). This enables
                // baggage traversal from the test body to the test case — required for
                // cross-process correlation via Activity.GetBaggageItem("tunit.test.id").
                if (executableTest.Context.Activity is { } testCaseActivity)
                {
                    Activity.Current = testCaseActivity;
                }

                testBodyActivity = TUnitActivitySource.StartActivity(
                    TUnitActivitySource.SpanTestBody);
            }
#endif
            try
            {
                if (testTimeout.HasValue)
                {
                    // Own the linked timeout source across the whole test lifecycle rather than letting
                    // TimeoutHelper dispose it on return. The body can hand Context.CancellationToken to
                    // app/user code (EF Core, Respawn, an ASP.NET host) that only touches it during
                    // teardown; disposing it the moment the body returned left those captured copies
                    // pointing at a disposed source, so a synchronous .WaitHandle wait in an After(Test)
                    // hook / OnDispose threw ObjectDisposedException (fixes #6339). It lives on the context
                    // and is disposed once by TestCoordinator after every teardown phase has run. A prior
                    // retry attempt's source (if any) is released here before the new one replaces it.
                    var context = executableTest.Context;
                    context.TimeoutCancellationSource?.Dispose();
                    var testCancellationToken = context.TestCancellationToken;
                    var testBodyTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(testCancellationToken);
                    context.TimeoutCancellationSource = testBodyTimeoutCts;

                    var timeoutMessage = $"Test '{context.Metadata.TestDetails.TestName}' timed out after {testTimeout.Value}";

                    await TimeoutHelper.ExecuteWithTimeoutAsync(
                        ct => ExecuteTestAsync(executableTest, ct).AsTask(),
                        testTimeout.Value,
                        testBodyTimeoutCts,
                        testCancellationToken,
                        timeoutMessage).ConfigureAwait(false);
                }
                else
                {
                    // Fast path: no timeout — invoke directly, with no timeout-specific CTS/TCS/WhenAny overhead
                    await ExecuteTestAsync(
                        executableTest,
                        executableTest.Context.TestCancellationToken).ConfigureAwait(false);
                }
            }
            catch
#if NET
            (Exception ex)
#endif
            {
#if NET
                TUnitActivitySource.RecordException(testBodyActivity, ex);
#endif
                throw;
            }
            finally
            {
#if NET
                TUnitActivitySource.StopActivity(testBodyActivity);
#endif
                executableTest.Context.Execution.TestEnd ??= DateTimeOffset.UtcNow;

                // The timeout path set Context.CancellationToken to the linked timeout token, which
                // is cancelled once the timeout fires. Restore the still-valid, non-cancelled outer
                // token — colocated here with the timeout call that mutated it — so the test-end event
                // receivers, After(Test)/AfterEvery(Test) hooks, and any retry back-off observe a live
                // token via the context property. (The source itself is kept alive on the context and
                // disposed later by TestCoordinator, so token copies captured mid-body stay valid — #6339.)
                if (testTimeout.HasValue)
                {
                    executableTest.Context.RestoreTestCancellationToken();
                }
            }

            executableTest.SetResult(executableTest.Context.IsTestCancellationRequested
                ? TestState.Cancelled
                : TestState.Passed);
        }
        catch (SkipTestException ex)
        {
            executableTest.SetResult(TestState.Skipped);
            // Surface the skip reason on the context so FinishTestActivity (now invoked from
            // TestCoordinator after the local capturedException is out of scope) can tag the span.
            executableTest.Context.SkipReason ??= ex.Reason;
            capturedException = ex;
        }
        catch (OperationCanceledException) when (executableTest.Context.IsTestCancellationRequested)
        {
            executableTest.SetResult(TestState.Cancelled);
        }
        catch (Exception ex)
        {
            executableTest.SetResult(TestState.Failed, ex);
            capturedException = ex;
        }
        finally
        {
            // After hooks must use CancellationToken.None to ensure cleanup runs even when cancelled
            // This matches the pattern used for After Class/Assembly hooks in TestCoordinator

            // Early stage test end receivers run before instance-level hooks
            var earlyStageExceptions = await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(executableTest.Context, CancellationToken.None, EventReceiverStage.Early).ConfigureAwait(false);

            var hookExceptions = hasTestHooks
                ? await _hookExecutor.ExecuteAfterTestHooksAsync(executableTest, CancellationToken.None).ConfigureAwait(false)
                : [];

            // Late stage test end receivers run after instance-level hooks (default behavior)
            var lateStageExceptions = await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(executableTest.Context, CancellationToken.None, EventReceiverStage.Late).ConfigureAwait(false);

            // Combine all exceptions from event receivers - defer allocation until needed
            IReadOnlyList<Exception> eventReceiverExceptions;
            if (earlyStageExceptions.Count > 0 || lateStageExceptions.Count > 0)
            {
                var combined = new List<Exception>(earlyStageExceptions.Count + lateStageExceptions.Count);
                combined.AddRange(earlyStageExceptions);
                combined.AddRange(lateStageExceptions);
                eventReceiverExceptions = combined;
            }
            else
            {
                eventReceiverExceptions = [];
            }

            if (hookExceptions.Count > 0 || eventReceiverExceptions.Count > 0)
            {
                hookException = new TestExecutionException(null, hookExceptions, eventReceiverExceptions);
            }

            // Keep cancellation open while a failed attempt hands control back to RetryHelper.
            // Closing it here would create a gap where Cancel() could be lost before retry handling begins.
            var canRetry = executableTest.Context.CurrentRetryAttempt
                < executableTest.Context.Metadata.TestDetails.RetryLimit
                && capturedException is not SkipTestException
                && (capturedException is not null || hookException is not null);

            if (!canRetry && executableTest.Context.CompleteTestCancellation())
            {
                executableTest.SetResult(TestState.Cancelled);
            }
        }

        if (capturedException is SkipTestException)
        {
            ExceptionDispatchInfo.Capture(capturedException).Throw();
        }
        else if (executableTest.Context.Execution.Result?.IsOverridden == true)
        {
            return;
        }
        else if (capturedException != null && hookException != null)
        {
            var combinedException = new TestExecutionException(capturedException,
                (hookException as TestExecutionException)?.HookExceptions ?? [],
                (hookException as TestExecutionException)?.EventReceiverExceptions ?? []);
            ExceptionDispatchInfo.Capture(combinedException).Throw();
        }
        else if (capturedException != null)
        {
            ExceptionDispatchInfo.Capture(capturedException).Throw();
        }
        else if (hookException != null)
        {
            ExceptionDispatchInfo.Capture(hookException).Throw();
        }
    }

    private static bool HasTestSessionHooks()
        => !Sources.BeforeTestSessionHooks.IsEmpty || !Sources.AfterTestSessionHooks.IsEmpty;

    private static bool HasAssemblyHooks(Assembly assembly)
        => !Sources.BeforeEveryAssemblyHooks.IsEmpty ||
           !Sources.AfterEveryAssemblyHooks.IsEmpty ||
           Sources.BeforeAssemblyHooks.ContainsKey(assembly) ||
           Sources.AfterAssemblyHooks.ContainsKey(assembly);

    private static bool HasClassHooks(Type testClass)
        => !Sources.BeforeEveryClassHooks.IsEmpty ||
           !Sources.AfterEveryClassHooks.IsEmpty ||
           ClassHookPresenceCache.GetOrAdd(testClass, static type =>
               HasHooksInHierarchy(type, Sources.BeforeClassHooks, Sources.AfterClassHooks));

    private static bool HasTestHooks(Type testClass)
        => !Sources.BeforeEveryTestHooks.IsEmpty ||
           !Sources.AfterEveryTestHooks.IsEmpty ||
           TestHookPresenceCache.GetOrAdd(testClass, static type =>
               HasHooksInHierarchy(type, Sources.BeforeTestHooks, Sources.AfterTestHooks));

    private static bool HasHooksInHierarchy<TBeforeHook, TAfterHook>(
        Type type,
        ConcurrentDictionary<Type, ConcurrentBag<LazyHookEntry<TBeforeHook>>> beforeHooks,
        ConcurrentDictionary<Type, ConcurrentBag<LazyHookEntry<TAfterHook>>> afterHooks)
        where TBeforeHook : HookMethod
        where TAfterHook : HookMethod
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (beforeHooks.ContainsKey(current) || afterHooks.ContainsKey(current))
            {
                return true;
            }

            if (current is { IsGenericType: true, IsGenericTypeDefinition: false })
            {
                var genericDefinition = current.GetGenericTypeDefinition();
                if (beforeHooks.ContainsKey(genericDefinition) || afterHooks.ContainsKey(genericDefinition))
                {
                    return true;
                }
            }
        }

        return false;
    }

#if NET
    internal static void FinishTestActivity(AbstractExecutableTest executableTest)
    {
        var activity = executableTest.Context.Activity;

        if (activity is null)
        {
            return;
        }

        var result = executableTest.Context.Execution.Result;

        // Use OTel test semantic convention values: pass, fail, skipped
        var statusValue = result?.State switch
        {
            TestState.Passed => "pass",
            TestState.Timeout => "fail",
            TestState.Cancelled => "fail",
            TestState.Failed => "fail",
            TestState.Skipped => "skipped",
            _ => "unknown"
        };
        activity.SetTag(TUnitActivitySource.TagTestCaseResultStatus, statusValue);

        if (executableTest.Context.CurrentRetryAttempt > 0)
        {
            activity.SetTag(TUnitActivitySource.TagTestRetryAttempt, executableTest.Context.CurrentRetryAttempt);
        }

        var skipReason = executableTest.Context.SkipReason
            ?? (result?.State == TestState.Skipped ? result.OverrideReason : null);

        if (!string.IsNullOrEmpty(skipReason))
        {
            // Skipped tests are not errors — leave status as Unset
            activity.SetTag(TUnitActivitySource.TagTestSkipReason, skipReason);
        }
        else if (result?.Exception is { } exception)
        {
            // RecordException sets Error status and error.type tag
            TUnitActivitySource.RecordException(activity, exception);
        }
        else if (result?.State is TestState.Failed or TestState.Timeout or TestState.Cancelled)
        {
            // Failing state with no captured exception (e.g. overridden result, cancellation
            // that did not surface as an exception). Still surface status/error.type so
            // backends render this as a failed span instead of a silent OK.
            activity.SetStatus(ActivityStatusCode.Error);
            activity.SetTag("error.type", result.State.ToString());
        }
        // Success: leave status as Unset per OTel instrumentation library conventions

        TUnitActivitySource.StopActivity(activity);
        executableTest.Context.Activity = null;
    }
#endif

    private static async ValueTask ExecuteTestAsync(AbstractExecutableTest executableTest, CancellationToken cancellationToken)
    {
        // Skip the actual test invocation for skipped tests
        if (executableTest.Context.Metadata.TestDetails.ClassInstance is SkippedTestInstance ||
            !string.IsNullOrEmpty(executableTest.Context.SkipReason))
        {
            return;
        }

        // Set the test start time when we actually begin executing the test
        executableTest.Context.TestStart = DateTimeOffset.UtcNow;

        // Set the cancellation token on the context so source-generated tests can access it
        executableTest.Context.SetCancellationToken(cancellationToken);

        if (executableTest.Context.InternalDiscoveredTest?.TestExecutor is { } testExecutor)
        {
            await testExecutor.ExecuteTest(executableTest.Context,
                () => new ValueTask(executableTest.InvokeTestAsync(
                    executableTest.Context.Metadata.TestDetails.ClassInstance,
                    executableTest.Context.Execution.CancellationToken))).ConfigureAwait(false);
        }
        else
        {
            await executableTest.InvokeTestAsync(
                executableTest.Context.Metadata.TestDetails.ClassInstance,
                executableTest.Context.Execution.CancellationToken).ConfigureAwait(false);
        }
    }

    private Func<Assembly, ValueTask<List<Exception>>>? ResolveAssemblyCleanup(
        Assembly assembly,
        bool hasHooks,
        Func<Assembly, ValueTask<List<Exception>>> hookFactory)
    {
        if (hasHooks)
        {
            return hookFactory;
        }

#if NET
        if (_hookExecutor.HasAssemblyActivity(assembly))
        {
            return _finishAssemblyActivityFactory;
        }
#endif

        return null;
    }

    private AfterClassExecutor? ResolveClassCleanup(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass,
        bool hasHooks,
        AfterClassExecutor hookFactory)
    {
        if (hasHooks)
        {
            return hookFactory;
        }

#if NET
        if (_hookExecutor.HasClassActivity(testClass))
        {
            return _finishClassActivityFactory;
        }
#endif

        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "The class cleanup delegate is invoked with the annotated testClass parameter.")]
    internal async Task<List<Exception>?> ExecuteAfterClassAssemblyHooks(AbstractExecutableTest executableTest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, Assembly testAssembly, CancellationToken cancellationToken)
    {
        var flags = _lifecycleCoordinator.DecrementAndCheckAfterHooks(testClass, testAssembly);

        if (!flags.ShouldExecuteAfterClass && !flags.ShouldExecuteAfterAssembly)
        {
            return null;
        }

        List<Exception>? exceptions = null;

        if (flags.ShouldExecuteAfterClass)
        {
            var afterClassFactory = ResolveClassCleanup(
                testClass,
                HasClassHooks(testClass),
                type => _hookExecutor.ExecuteAfterClassHooksAsync(type, cancellationToken));

            if (afterClassFactory is not null)
            {
                // Use AfterHookPairTracker to prevent double execution if already triggered by cancellation
                var classExceptions = await _afterHookPairTracker.GetOrCreateAfterClassTask(testClass, afterClassFactory).ConfigureAwait(false);
                if (classExceptions.Count > 0)
                {
                    (exceptions ??= []).AddRange(classExceptions);
                }
            }
        }

        if (flags.ShouldExecuteAfterAssembly)
        {
            var afterAssemblyFactory = ResolveAssemblyCleanup(
                testAssembly,
                HasAssemblyHooks(testAssembly),
                assembly => _hookExecutor.ExecuteAfterAssemblyHooksAsync(assembly, cancellationToken));

            if (afterAssemblyFactory is not null)
            {
                // Use AfterHookPairTracker to prevent double execution if already triggered by cancellation
                var assemblyExceptions = await _afterHookPairTracker.GetOrCreateAfterAssemblyTask(
                    testAssembly,
                    afterAssemblyFactory).ConfigureAwait(false);
                if (assemblyExceptions.Count > 0)
                {
                    (exceptions ??= []).AddRange(assemblyExceptions);
                }
            }
        }

        return exceptions;
    }

    /// <summary>
    /// Execute session-level after hooks once at the end of test execution.
    /// Returns any exceptions that occurred during hook execution.
    /// Uses AfterHookPairTracker to prevent double execution if already triggered by cancellation.
    /// </summary>
    public async Task<List<Exception>> ExecuteAfterTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        // Use AfterHookPairTracker to prevent double execution if already triggered by cancellation
        var exceptions = await _afterHookPairTracker.GetOrCreateAfterTestSessionTask(
            () => _hookExecutor.ExecuteAfterTestSessionHooksAsync(cancellationToken)).ConfigureAwait(false);

        return exceptions;
    }

    /// <summary>
    /// Execute discovery-level before hooks.
    /// </summary>
    public ValueTask ExecuteBeforeTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        return _hookExecutor.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken);
    }

    /// <summary>
    /// Execute discovery-level after hooks.
    /// </summary>
    public ValueTask ExecuteAfterTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        return _hookExecutor.ExecuteAfterTestDiscoveryHooksAsync(cancellationToken);
    }

#if NET
    /// <inheritdoc cref="HookExecutor.TryStartSessionActivity"/>
    internal void TryStartSessionActivity() => _hookExecutor.TryStartSessionActivity();
#endif

    /// <summary>
    /// Get the context provider for accessing test contexts.
    /// </summary>
    public IContextProvider GetContextProvider()
    {
        return _contextProvider;
    }

    internal static async Task DisposeTestInstance(AbstractExecutableTest test)
    {
        // Dispose the test instance if it's disposable
        if (test.Context.Metadata.TestDetails.ClassInstance is not SkippedTestInstance)
        {
            try
            {
                var instance = test.Context.Metadata.TestDetails.ClassInstance;

                switch (instance)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
            catch
            {
                // Swallow disposal errors - they shouldn't fail the test
            }
        }
    }
}
