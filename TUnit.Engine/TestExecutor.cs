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
            () => new ValueTask<List<Exception>>(_hookExecutor.ExecuteAfterTestSessionHooksAsync(CancellationToken.None).AsTask()));
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
                (assembly) => new ValueTask<List<Exception>>(_hookExecutor.ExecuteAfterAssemblyHooksAsync(assembly, CancellationToken.None).AsTask()));

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

            // Initialize test objects (IAsyncInitializer) AFTER BeforeClass hooks
            // This ensures resources like Docker containers are not started until needed
            await testInitializer.InitializeTestObjectsAsync(executableTest, cancellationToken).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            // Early stage test start receivers run before instance-level hooks
            await _eventReceiverOrchestrator.InvokeTestStartEventReceiversAsync(executableTest.Context, cancellationToken, EventReceiverStage.Early).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            await Timings.Record("BeforeTest", executableTest.Context,
                () => _hookExecutor.ExecuteBeforeTestHooksAsync(executableTest, cancellationToken)).ConfigureAwait(false);

            // Late stage test start receivers run after instance-level hooks (default behavior)
            await _eventReceiverOrchestrator.InvokeTestStartEventReceiversAsync(executableTest.Context, cancellationToken, EventReceiverStage.Late).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            // Only the test body is subject to the [Timeout] — hooks and data source
            // initialization run outside the timeout scope (fixes #4772)
            try
            {
                if (testTimeout.HasValue)
                {
                    var timeoutMessage = $"Test '{executableTest.Context.Metadata.TestDetails.TestName}' timed out after {testTimeout.Value}";

                    await TimeoutHelper.ExecuteWithTimeoutAsync(
                        ct => ExecuteTestAsync(executableTest, ct).AsTask(),
                        testTimeout,
                        cancellationToken,
                        timeoutMessage).ConfigureAwait(false);
                }
                else
                {
                    await ExecuteTestAsync(executableTest, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                executableTest.Context.Execution.TestEnd ??= DateTimeOffset.UtcNow;
            }

            executableTest.SetResult(TestState.Passed);
        }
        catch (SkipTestException ex)
        {
            executableTest.SetResult(TestState.Skipped);
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

            IReadOnlyList<Exception> hookExceptions = [];
            await Timings.Record("AfterTest", executableTest.Context, (Func<Task>)(async () =>
            {
                hookExceptions = await _hookExecutor.ExecuteAfterTestHooksAsync(executableTest, CancellationToken.None).ConfigureAwait(false);
            })).ConfigureAwait(false);

            // Late stage test end receivers run after instance-level hooks (default behavior)
            var lateStageExceptions = await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(executableTest.Context, CancellationToken.None, EventReceiverStage.Late).ConfigureAwait(false);

            // Combine all exceptions from event receivers
            var eventReceiverExceptions = new List<Exception>(earlyStageExceptions.Count + lateStageExceptions.Count);
            eventReceiverExceptions.AddRange(earlyStageExceptions);
            eventReceiverExceptions.AddRange(lateStageExceptions);

            if (hookExceptions.Count > 0 || eventReceiverExceptions.Count > 0)
            {
                hookException = new TestExecutionException(null, hookExceptions, eventReceiverExceptions);
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
                async () => await executableTest.InvokeTestAsync(executableTest.Context.Metadata.TestDetails.ClassInstance, cancellationToken)).ConfigureAwait(false);
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
                (assembly) => new ValueTask<List<Exception>>(_hookExecutor.ExecuteAfterAssemblyHooksAsync(assembly, cancellationToken).AsTask())).ConfigureAwait(false);
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
            () => new ValueTask<List<Exception>>(_hookExecutor.ExecuteAfterTestSessionHooksAsync(cancellationToken).AsTask())).ConfigureAwait(false);

        return exceptions;
    }

    /// <summary>
    /// Execute discovery-level before hooks.
    /// </summary>
    public async Task ExecuteBeforeTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        await _hookExecutor.ExecuteBeforeTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute discovery-level after hooks.
    /// </summary>
    public async Task ExecuteAfterTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        await _hookExecutor.ExecuteAfterTestDiscoveryHooksAsync(cancellationToken).ConfigureAwait(false);
    }

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
