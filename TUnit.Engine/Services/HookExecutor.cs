using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Services;
using TUnit.Engine.Interfaces;

namespace TUnit.Engine.Services;

/// <summary>
/// Responsible for executing hooks and event receivers with proper context hierarchy.
/// Merges the functionality of hooks and first/last event receivers for unified lifecycle management.
/// Follows Single Responsibility Principle - only handles hook and event receiver execution.
/// </summary>
internal sealed class HookExecutor
{
    private readonly IHookCollectionService _hookCollectionService;
    private readonly IContextProvider _contextProvider;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;

    public HookExecutor(
        IHookCollectionService hookCollectionService,
        IContextProvider contextProvider,
        EventReceiverOrchestrator eventReceiverOrchestrator)
    {
        _hookCollectionService = hookCollectionService;
        _contextProvider = contextProvider;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
    }

    public async Task ExecuteBeforeTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeTestSessionHooksAsync().ConfigureAwait(false);

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestSessionContext.RestoreExecutionContext();
                await hook(_contextProvider.TestSessionContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Wrap hook exceptions in specific exception types
                // This allows the test runner to handle hook failures appropriately
                throw new BeforeTestSessionException($"BeforeTestSession hook failed", ex);
            }
        }
    }

    /// <summary>
    /// Execute before test session hooks AND first test in session event receivers for a specific test context.
    /// This consolidates both lifecycle mechanisms into a single call.
    /// </summary>
    public async Task ExecuteBeforeTestSessionHooksAsync(TestContext testContext, CancellationToken cancellationToken)
    {
        // Execute regular before session hooks
        await ExecuteBeforeTestSessionHooksAsync(cancellationToken).ConfigureAwait(false);

        // Also execute first test in session event receivers (these run only once via internal task coordination)
        await _eventReceiverOrchestrator.InvokeFirstTestInSessionEventReceiversAsync(
            testContext,
            testContext.ClassContext.AssemblyContext.TestSessionContext,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAfterTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterTestSessionHooksAsync().ConfigureAwait(false);
        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestSessionContext.RestoreExecutionContext();
                await hook(_contextProvider.TestSessionContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Wrap hook exceptions in specific exception types
                throw new AfterTestSessionException($"AfterTestSession hook failed", ex);
            }
        }
    }

    public async Task ExecuteBeforeAssemblyHooksAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeAssemblyHooksAsync(assembly).ConfigureAwait(false);
        foreach (var hook in hooks)
        {
            try
            {
                var context = _contextProvider.GetOrCreateAssemblyContext(assembly);
                context.RestoreExecutionContext();
                await hook(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new BeforeAssemblyException($"BeforeAssembly hook failed", ex);
            }
        }
    }

    /// <summary>
    /// Execute before assembly hooks AND first test in assembly event receivers for a specific test context.
    /// This consolidates both lifecycle mechanisms into a single call.
    /// </summary>
    public async Task ExecuteBeforeAssemblyHooksAsync(TestContext testContext, CancellationToken cancellationToken)
    {
        var assembly = testContext.TestDetails.ClassType.Assembly;

        // Execute regular before assembly hooks
        await ExecuteBeforeAssemblyHooksAsync(assembly, cancellationToken).ConfigureAwait(false);

        // Also execute first test in assembly event receivers
        await _eventReceiverOrchestrator.InvokeFirstTestInAssemblyEventReceiversAsync(
            testContext,
            testContext.ClassContext.AssemblyContext,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAfterAssemblyHooksAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterAssemblyHooksAsync(assembly).ConfigureAwait(false);
        foreach (var hook in hooks)
        {
            try
            {
                var context = _contextProvider.GetOrCreateAssemblyContext(assembly);
                context.RestoreExecutionContext();
                await hook(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new AfterAssemblyException($"AfterAssembly hook failed", ex);
            }
        }
    }

    public async Task ExecuteBeforeClassHooksAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeClassHooksAsync(testClass).ConfigureAwait(false);
        foreach (var hook in hooks)
        {
            try
            {
                var context = _contextProvider.GetOrCreateClassContext(testClass);
                context.RestoreExecutionContext();
                await hook(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new BeforeClassException($"BeforeClass hook failed", ex);
            }
        }
    }

    /// <summary>
    /// Execute before class hooks AND first test in class event receivers for a specific test context.
    /// This consolidates both lifecycle mechanisms into a single call.
    /// </summary>
    public async Task ExecuteBeforeClassHooksAsync(TestContext testContext, CancellationToken cancellationToken)
    {
        var testClass = testContext.TestDetails.ClassType;

        // Execute regular before class hooks
        await ExecuteBeforeClassHooksAsync(testClass, cancellationToken).ConfigureAwait(false);

        // Also execute first test in class event receivers
        await _eventReceiverOrchestrator.InvokeFirstTestInClassEventReceiversAsync(
            testContext,
            testContext.ClassContext,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAfterClassHooksAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterClassHooksAsync(testClass).ConfigureAwait(false);
        foreach (var hook in hooks)
        {
            try
            {
                var context = _contextProvider.GetOrCreateClassContext(testClass);
                context.RestoreExecutionContext();
                await hook(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new AfterClassException($"AfterClass hook failed", ex);
            }
        }
    }

    public async Task ExecuteBeforeTestHooksAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        var testClassType = test.Metadata.TestClassType;

        // Execute Before(Test) hooks specific to this test
        var beforeTestHooks = await _hookCollectionService.CollectBeforeTestHooksAsync(testClassType).ConfigureAwait(false);
        foreach (var hook in beforeTestHooks)
        {
            try
            {
                test.Context.RestoreExecutionContext();
                await hook(test.Context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new BeforeTestException($"BeforeTest hook failed", ex);
            }
        }

        // Execute BeforeEvery(Test) hooks (global test hooks)
        var beforeEveryTestHooks = await _hookCollectionService.CollectBeforeEveryTestHooksAsync(testClassType).ConfigureAwait(false);
        foreach (var hook in beforeEveryTestHooks)
        {
            try
            {
                test.Context.RestoreExecutionContext();
                await hook(test.Context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new BeforeTestException($"BeforeEveryTest hook failed", ex);
            }
        }
    }

    public async Task ExecuteAfterTestHooksAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        var testClassType = test.Metadata.TestClassType;

        // Execute After(Test) hooks specific to this test
        var afterTestHooks = await _hookCollectionService.CollectAfterTestHooksAsync(testClassType).ConfigureAwait(false);
        foreach (var hook in afterTestHooks)
        {
            try
            {
                test.Context.RestoreExecutionContext();
                await hook(test.Context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new AfterTestException($"AfterTest hook failed", ex);
            }
        }

        // Execute AfterEvery(Test) hooks (global test hooks)
        var afterEveryTestHooks = await _hookCollectionService.CollectAfterEveryTestHooksAsync(testClassType).ConfigureAwait(false);
        foreach (var hook in afterEveryTestHooks)
        {
            try
            {
                test.Context.RestoreExecutionContext();
                await hook(test.Context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new AfterTestException($"AfterEveryTest hook failed", ex);
            }
        }
    }

    public async Task ExecuteBeforeTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeTestDiscoveryHooksAsync().ConfigureAwait(false);
        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.BeforeTestDiscoveryContext.RestoreExecutionContext();
                await hook(_contextProvider.BeforeTestDiscoveryContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new BeforeTestDiscoveryException($"BeforeTestDiscovery hook failed", ex);
            }
        }
    }

    public async Task ExecuteAfterTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterTestDiscoveryHooksAsync().ConfigureAwait(false);
        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestDiscoveryContext.RestoreExecutionContext();
                await hook(_contextProvider.TestDiscoveryContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new AfterTestDiscoveryException($"AfterTestDiscovery hook failed", ex);
            }
        }
    }
}
