using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
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
    private readonly IContextProvider _contextProvider;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;

    public TestExecutor(
        HookExecutor hookExecutor,
        TestLifecycleCoordinator lifecycleCoordinator,
        BeforeHookTaskCache beforeHookTaskCache,
        IContextProvider contextProvider,
        EventReceiverOrchestrator eventReceiverOrchestrator)
    {
        _hookExecutor = hookExecutor;
        _lifecycleCoordinator = lifecycleCoordinator;
        _beforeHookTaskCache = beforeHookTaskCache;
        _contextProvider = contextProvider;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
    }


    /// <summary>
    /// Ensures that Before(TestSession) hooks have been executed.
    /// This is called before creating test instances to ensure resources are available.
    /// </summary>
    public async Task EnsureTestSessionHooksExecutedAsync()
    {
        // Get or create and cache Before hooks - these run only once
        await _beforeHookTaskCache.GetOrCreateBeforeTestSessionTask(() =>
            _hookExecutor.ExecuteBeforeTestSessionHooksAsync(CancellationToken.None)).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a test executor delegate that wraps the provided executor with hook orchestration.
    /// Uses focused services that follow SRP to manage lifecycle and execution.
    /// </summary>
    public async ValueTask ExecuteAsync(AbstractExecutableTest executableTest, CancellationToken cancellationToken)
    {

        var testClass = executableTest.Metadata.TestClassType;
        var testAssembly = testClass.Assembly;

        Exception? capturedException = null;
        Exception? hookException = null;

        try
        {
            await EnsureTestSessionHooksExecutedAsync().ConfigureAwait(false);

            await _eventReceiverOrchestrator.InvokeFirstTestInSessionEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext.AssemblyContext.TestSessionContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.AssemblyContext.TestSessionContext.RestoreExecutionContext();

            await _beforeHookTaskCache.GetOrCreateBeforeAssemblyTask(testAssembly, assembly => _hookExecutor.ExecuteBeforeAssemblyHooksAsync(assembly, CancellationToken.None))
                .ConfigureAwait(false);

            await _eventReceiverOrchestrator.InvokeFirstTestInAssemblyEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext.AssemblyContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.AssemblyContext.RestoreExecutionContext();

            await _beforeHookTaskCache.GetOrCreateBeforeClassTask(testClass, _ => _hookExecutor.ExecuteBeforeClassHooksAsync(testClass, CancellationToken.None))
                .ConfigureAwait(false);

            await _eventReceiverOrchestrator.InvokeFirstTestInClassEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.RestoreExecutionContext();

            await _hookExecutor.ExecuteBeforeTestHooksAsync(executableTest, cancellationToken).ConfigureAwait(false);

            await _eventReceiverOrchestrator.InvokeTestStartEventReceiversAsync(executableTest.Context, cancellationToken).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            // Timeout is now enforced at TestCoordinator level (wrapping entire lifecycle)
            await ExecuteTestAsync(executableTest, cancellationToken).ConfigureAwait(false);

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
            var hookExceptions = await _hookExecutor.ExecuteAfterTestHooksAsync(executableTest, cancellationToken).ConfigureAwait(false);
            var eventReceiverExceptions = await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(executableTest.Context, cancellationToken).ConfigureAwait(false);

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
            var classExceptions = await _hookExecutor.ExecuteAfterClassHooksAsync(testClass, cancellationToken).ConfigureAwait(false);
            exceptions.AddRange(classExceptions);
        }

        if (flags.ShouldExecuteAfterAssembly)
        {
            var assemblyExceptions = await _hookExecutor.ExecuteAfterAssemblyHooksAsync(testAssembly, cancellationToken).ConfigureAwait(false);
            exceptions.AddRange(assemblyExceptions);
        }

        return exceptions;
    }

    /// <summary>
    /// Execute session-level after hooks once at the end of test execution.
    /// Returns any exceptions that occurred during hook execution.
    /// </summary>
    public async Task<List<Exception>> ExecuteAfterTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        return await _hookExecutor.ExecuteAfterTestSessionHooksAsync(cancellationToken).ConfigureAwait(false);
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
