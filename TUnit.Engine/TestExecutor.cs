using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Simple orchestrator that composes focused services to manage test execution flow.
/// Follows Single Responsibility Principle and SOLID principles.
/// </summary>
internal class TestExecutor : IDisposable
{
    private readonly HookExecutor _hookExecutor;
    private readonly TestLifecycleCoordinator _lifecycleCoordinator;
    private readonly BeforeHookTaskCache _beforeHookTaskCache;
    private readonly IContextProvider _contextProvider;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;

    // Cached delegates to prevent lambda capture issues
    private readonly Func<Task> _executeBeforeTestSessionHooks;
    private readonly Func<Assembly, Task> _executeBeforeAssemblyHooks;
    private readonly Func<Type, Task> _executeBeforeClassHooks;

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

        // Initialize cached delegates once to avoid lambda capture issues
        _executeBeforeTestSessionHooks = () => _hookExecutor.ExecuteBeforeTestSessionHooksAsync(CancellationToken.None);
        _executeBeforeAssemblyHooks = assembly => _hookExecutor.ExecuteBeforeAssemblyHooksAsync(assembly, CancellationToken.None);
#pragma warning disable IL2067
        _executeBeforeClassHooks = cls => _hookExecutor.ExecuteBeforeClassHooksAsync(cls, CancellationToken.None);
#pragma warning restore IL2067
    }


    /// <summary>
    /// Creates a test executor delegate that wraps the provided executor with hook orchestration.
    /// Uses focused services that follow SRP to manage lifecycle and execution.
    /// </summary>
    public async Task ExecuteAsync(AbstractExecutableTest executableTest, CancellationToken cancellationToken)
    {

        var testClass = executableTest.Metadata.TestClassType;
        var testAssembly = testClass.Assembly;

        try
        {
            // Get or create and cache Before hooks - these run only once
            // We use cached delegates to prevent lambda capture issues
            // Event receivers will be handled separately with their own internal coordination
            await _beforeHookTaskCache.GetOrCreateBeforeTestSessionTask(_executeBeforeTestSessionHooks).ConfigureAwait(false);

            // Event receivers have their own internal coordination to run once
            await _eventReceiverOrchestrator.InvokeFirstTestInSessionEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext.AssemblyContext.TestSessionContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.AssemblyContext.TestSessionContext.RestoreExecutionContext();

            await _beforeHookTaskCache.GetOrCreateBeforeAssemblyTask(testAssembly, _executeBeforeAssemblyHooks).ConfigureAwait(false);

            // Event receivers for first test in assembly
            await _eventReceiverOrchestrator.InvokeFirstTestInAssemblyEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext.AssemblyContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.AssemblyContext.RestoreExecutionContext();

            await _beforeHookTaskCache.GetOrCreateBeforeClassTask(testClass, _executeBeforeClassHooks).ConfigureAwait(false);

            // Event receivers for first test in class
            await _eventReceiverOrchestrator.InvokeFirstTestInClassEventReceiversAsync(
                executableTest.Context,
                executableTest.Context.ClassContext,
                cancellationToken).ConfigureAwait(false);

            executableTest.Context.ClassContext.RestoreExecutionContext();

            await _hookExecutor.ExecuteBeforeTestHooksAsync(executableTest, cancellationToken).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            // Only wrap the actual test execution with timeout, not the hooks
            var testTimeout = executableTest.Context.TestDetails.Timeout;
            var timeoutMessage = testTimeout.HasValue
                ? $"Test '{executableTest.Context.TestDetails.TestName}' execution timed out after {testTimeout.Value}"
                : null;

            await TimeoutHelper.ExecuteWithTimeoutAsync(
                ct => ExecuteTestAsync(executableTest, ct),
                testTimeout,
                cancellationToken,
                timeoutMessage).ConfigureAwait(false);

            await _hookExecutor.ExecuteAfterTestHooksAsync(executableTest, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Always decrement counters and run After hooks if we're the last test
            await ExecuteAfterHooksBasedOnLifecycle(testClass, testAssembly, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ExecuteTestAsync(AbstractExecutableTest executableTest, CancellationToken cancellationToken)
    {
        // Skip the actual test invocation for skipped tests
        if (executableTest.Context.TestDetails.ClassInstance is SkippedTestInstance ||
            !string.IsNullOrEmpty(executableTest.Context.SkipReason))
        {
            return;
        }

        if (executableTest.Context.InternalDiscoveredTest?.TestExecutor is { } testExecutor)
        {
            await testExecutor.ExecuteTest(executableTest.Context,
                async () => await executableTest.InvokeTestAsync(executableTest.Context.TestDetails.ClassInstance, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            await executableTest.InvokeTestAsync(executableTest.Context.TestDetails.ClassInstance, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteAfterHooksBasedOnLifecycle(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, Assembly testAssembly, CancellationToken cancellationToken)
    {
        var flags = _lifecycleCoordinator.DecrementAndCheckAfterHooks(testClass, testAssembly);

        if (flags.ShouldExecuteAfterClass)
        {
            await _hookExecutor.ExecuteAfterClassHooksAsync(testClass, cancellationToken).ConfigureAwait(false);
        }

        if (flags.ShouldExecuteAfterAssembly)
        {
            await _hookExecutor.ExecuteAfterAssemblyHooksAsync(testAssembly, cancellationToken).ConfigureAwait(false);
        }

        if (flags.ShouldExecuteAfterTestSession)
        {
            await _hookExecutor.ExecuteAfterTestSessionHooksAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Execute session-level after hooks once at the end of test execution.
    /// </summary>
    public async Task ExecuteAfterTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        await _hookExecutor.ExecuteAfterTestSessionHooksAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Register tests for lifecycle coordination. Should be called after filtering.
    /// </summary>
    public void RegisterTests(IEnumerable<AbstractExecutableTest> tests)
    {
        _lifecycleCoordinator.RegisterTests(tests);
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

    public void Dispose()
    {
        _lifecycleCoordinator.Dispose();
        _beforeHookTaskCache.Dispose();
    }
}
