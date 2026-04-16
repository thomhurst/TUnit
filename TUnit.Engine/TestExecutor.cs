using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;
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
    private readonly HookExecutor _hookExecutor;
    private readonly TestLifecycleCoordinator _lifecycleCoordinator;
    private readonly BeforeHookTaskCache _beforeHookTaskCache;
    private readonly AfterHookPairTracker _afterHookPairTracker;
    private readonly IContextProvider _contextProvider;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;

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
    }


    /// <summary>
    /// Ensures that Before(TestSession) hooks have been executed.
    /// This is called before creating test instances to ensure resources are available.
    /// Registers the corresponding After(TestSession) hook to run on cancellation.
    /// </summary>
    public async Task EnsureTestSessionHooksExecutedAsync(CancellationToken cancellationToken)
    {
        // Get or create and cache Before hooks - these run only once
        await _beforeHookTaskCache.GetOrCreateBeforeTestSessionTask(
            ct => _hookExecutor.ExecuteBeforeTestSessionHooksAsync(ct),
            cancellationToken).ConfigureAwait(false);

        // Register After Session hook to run on cancellation (guarantees cleanup)
        _afterHookPairTracker.RegisterAfterTestSessionHook(
            cancellationToken,
            () => _hookExecutor.ExecuteAfterTestSessionHooksAsync(CancellationToken.None));
    }

    /// <summary>
    /// Creates a test executor delegate that wraps the provided executor with hook orchestration.
    /// Uses focused services that follow SRP to manage lifecycle and execution.
    /// </summary>
    public async ValueTask ExecuteAsync(AbstractExecutableTest executableTest, TestInitializer testInitializer, CancellationToken cancellationToken, TimeSpan? testTimeout = null)
    {

        var testClass = executableTest.Metadata.TestClassType;
        var testAssembly = testClass.Assembly;

        Exception? capturedException = null;
        Exception? hookException = null;

        try
        {
            await EnsureTestSessionHooksExecutedAsync(cancellationToken).ConfigureAwait(false);

            await _eventReceiverOrchestrator.InvokeFirstTestInSessionEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext.AssemblyContext.TestSessionContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.AssemblyContext.TestSessionContext.RestoreExecutionContext();

            await _beforeHookTaskCache.GetOrCreateBeforeAssemblyTask(
                testAssembly,
                (assembly, ct) => _hookExecutor.ExecuteBeforeAssemblyHooksAsync(assembly, ct),
                cancellationToken).ConfigureAwait(false);

            // Register After Assembly hook to run on cancellation (guarantees cleanup)
            _afterHookPairTracker.RegisterAfterAssemblyHook(
                testAssembly,
                cancellationToken,
                (assembly) => _hookExecutor.ExecuteAfterAssemblyHooksAsync(assembly, CancellationToken.None));

            await _eventReceiverOrchestrator.InvokeFirstTestInAssemblyEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext.AssemblyContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.AssemblyContext.RestoreExecutionContext();

            await _beforeHookTaskCache.GetOrCreateBeforeClassTask(testClass, _hookExecutor, cancellationToken).ConfigureAwait(false);

            // Register After Class hook to run on cancellation (guarantees cleanup)
            _afterHookPairTracker.RegisterAfterClassHook(testClass, _hookExecutor, cancellationToken);

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

            // Initialize test objects (IAsyncInitializer) AFTER BeforeClass hooks
            // and after the test case activity starts. Per-test objects are traced
            // under the test case; shared objects under session/assembly/class.
            await testInitializer.InitializeTestObjectsAsync(executableTest, cancellationToken).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            // Early stage test start receivers run before instance-level hooks
            await _eventReceiverOrchestrator.InvokeTestStartEventReceiversAsync(executableTest.Context, cancellationToken, EventReceiverStage.Early).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            await _hookExecutor.ExecuteBeforeTestHooksAsync(executableTest, cancellationToken).ConfigureAwait(false);

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
                    var timeoutMessage = $"Test '{executableTest.Context.Metadata.TestDetails.TestName}' timed out after {testTimeout.Value}";

                    await TimeoutHelper.ExecuteWithTimeoutAsync(
                        ct => ExecuteTestAsync(executableTest, ct).AsTask(),
                        testTimeout.Value,
                        cancellationToken,
                        timeoutMessage).ConfigureAwait(false);
                }
                else
                {
                    // Fast path: no timeout — invoke directly, no CTS/TCS/WhenAny overhead
                    await ExecuteTestAsync(executableTest, cancellationToken).ConfigureAwait(false);
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
            }

            executableTest.SetResult(TestState.Passed);
        }
        catch (SkipTestException ex)
        {
            executableTest.SetResult(TestState.Skipped);
            // Surface the skip reason on the context so FinishTestActivity (now invoked from
            // TestCoordinator after the local capturedException is out of scope) can tag the span.
            executableTest.Context.SkipReason ??= ex.Reason;
            capturedException = ex;
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

            var hookExceptions = await _hookExecutor.ExecuteAfterTestHooksAsync(executableTest, CancellationToken.None).ConfigureAwait(false);

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

#if NET
#endif
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
        executableTest.Context.CancellationToken = cancellationToken;

        if (executableTest.Context.InternalDiscoveredTest?.TestExecutor is { } testExecutor)
        {
            await testExecutor.ExecuteTest(executableTest.Context,
                () => new ValueTask(executableTest.InvokeTestAsync(executableTest.Context.Metadata.TestDetails.ClassInstance, cancellationToken))).ConfigureAwait(false);
        }
        else
        {
            await executableTest.InvokeTestAsync(executableTest.Context.Metadata.TestDetails.ClassInstance, cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<List<Exception>> ExecuteAfterClassAssemblyHooks(AbstractExecutableTest executableTest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, Assembly testAssembly, CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();
        var flags = _lifecycleCoordinator.DecrementAndCheckAfterHooks(testClass, testAssembly);

        if (flags.ShouldExecuteAfterClass)
        {
            // Use AfterHookPairTracker to prevent double execution if already triggered by cancellation
            var classExceptions = await _afterHookPairTracker.GetOrCreateAfterClassTask(testClass, _hookExecutor, cancellationToken).ConfigureAwait(false);
            exceptions.AddRange(classExceptions);
        }

        if (flags.ShouldExecuteAfterAssembly)
        {
            // Use AfterHookPairTracker to prevent double execution if already triggered by cancellation
            var assemblyExceptions = await _afterHookPairTracker.GetOrCreateAfterAssemblyTask(
                testAssembly,
                (assembly) => _hookExecutor.ExecuteAfterAssemblyHooksAsync(assembly, cancellationToken)).ConfigureAwait(false);
            exceptions.AddRange(assemblyExceptions);
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
