using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
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
    public TestExecutor(
        HookExecutor hookExecutor,
        TestLifecycleCoordinator lifecycleCoordinator,
        BeforeHookTaskCache beforeHookTaskCache,
        IContextProvider contextProvider)
    {
        _hookExecutor = hookExecutor;
        _lifecycleCoordinator = lifecycleCoordinator;
        _beforeHookTaskCache = beforeHookTaskCache;
        _contextProvider = contextProvider;
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
            // Note: Using the consolidated hook methods that include both hooks and event receivers
            await _beforeHookTaskCache.GetOrCreateBeforeTestSessionTask(
                () => _hookExecutor.ExecuteBeforeTestSessionHooksAsync(executableTest.Context, cancellationToken)).ConfigureAwait(false);

            executableTest.Context.ClassContext.AssemblyContext.TestSessionContext.RestoreExecutionContext();

            await _beforeHookTaskCache.GetOrCreateBeforeAssemblyTask(testAssembly,
                assembly => _hookExecutor.ExecuteBeforeAssemblyHooksAsync(executableTest.Context, cancellationToken)).ConfigureAwait(false);

            executableTest.Context.ClassContext.AssemblyContext.RestoreExecutionContext();

            await _beforeHookTaskCache.GetOrCreateBeforeClassTask(testClass,
                cls => _hookExecutor.ExecuteBeforeClassHooksAsync(executableTest.Context, cancellationToken)).ConfigureAwait(false);

            executableTest.Context.ClassContext.RestoreExecutionContext();

            await _hookExecutor.ExecuteBeforeTestHooksAsync(executableTest, cancellationToken).ConfigureAwait(false);

            executableTest.Context.RestoreExecutionContext();

            await ExecuteTestAsync(executableTest, cancellationToken);

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
    /// Execute session-level before hooks once at the start of test execution.
    /// </summary>
    public async Task ExecuteBeforeTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        await _hookExecutor.ExecuteBeforeTestSessionHooksAsync(cancellationToken).ConfigureAwait(false);
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
