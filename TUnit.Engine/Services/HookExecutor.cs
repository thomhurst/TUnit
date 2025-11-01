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

    public async ValueTask ExecuteBeforeTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeTestSessionHooksAsync().ConfigureAwait(false);

        if (hooks.Count == 0)
        {
            return;
        }

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
                throw new BeforeTestSessionException("BeforeTestSession hook failed", ex);
            }
        }
    }

    public async ValueTask<List<Exception>> ExecuteAfterTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();
        var hooks = await _hookCollectionService.CollectAfterTestSessionHooksAsync().ConfigureAwait(false);

        if (hooks.Count == 0)
        {
            return exceptions;
        }

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestSessionContext.RestoreExecutionContext();
                await hook(_contextProvider.TestSessionContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Collect hook exceptions instead of throwing immediately
                // This allows all hooks to run even if some fail
                exceptions.Add(new AfterTestSessionException("AfterTestSession hook failed", ex));
            }
        }

        return exceptions;
    }

    public async ValueTask ExecuteBeforeAssemblyHooksAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeAssemblyHooksAsync(assembly).ConfigureAwait(false);

        if (hooks.Count == 0)
        {
            return;
        }

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
                throw new BeforeAssemblyException("BeforeAssembly hook failed", ex);
            }
        }
    }

    public async ValueTask<List<Exception>> ExecuteAfterAssemblyHooksAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();
        var hooks = await _hookCollectionService.CollectAfterAssemblyHooksAsync(assembly).ConfigureAwait(false);

        if (hooks.Count == 0)
        {
            return exceptions;
        }

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
                // Collect hook exceptions instead of throwing immediately
                // This allows all hooks to run even if some fail
                exceptions.Add(new AfterAssemblyException("AfterAssembly hook failed", ex));
            }
        }

        return exceptions;
    }

    public async ValueTask ExecuteBeforeClassHooksAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeClassHooksAsync(testClass).ConfigureAwait(false);

        if (hooks.Count == 0)
        {
            return;
        }

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
                throw new BeforeClassException("BeforeClass hook failed", ex);
            }
        }
    }

    public async ValueTask<List<Exception>> ExecuteAfterClassHooksAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();
        var hooks = await _hookCollectionService.CollectAfterClassHooksAsync(testClass).ConfigureAwait(false);

        if (hooks.Count == 0)
        {
            return exceptions;
        }

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
                // Collect hook exceptions instead of throwing immediately
                // This allows all hooks to run even if some fail
                exceptions.Add(new AfterClassException("AfterClass hook failed", ex));
            }
        }

        return exceptions;
    }

    public async ValueTask ExecuteBeforeTestHooksAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        var testClassType = test.Metadata.TestClassType;

        // Execute BeforeEvery(Test) hooks first (global test hooks run before specific hooks)
        var beforeEveryTestHooks = await _hookCollectionService.CollectBeforeEveryTestHooksAsync(testClassType).ConfigureAwait(false);

        if (beforeEveryTestHooks.Count > 0)
        {
            foreach (var hook in beforeEveryTestHooks)
            {
                try
                {
                    test.Context.RestoreExecutionContext();
                    await hook(test.Context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new BeforeTestException("BeforeEveryTest hook failed", ex);
                }
            }
        }

        // Execute Before(Test) hooks after BeforeEvery hooks
        var beforeTestHooks = await _hookCollectionService.CollectBeforeTestHooksAsync(testClassType).ConfigureAwait(false);

        if (beforeTestHooks.Count > 0)
        {
            foreach (var hook in beforeTestHooks)
            {
                try
                {
                    test.Context.RestoreExecutionContext();
                    await hook(test.Context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new BeforeTestException("BeforeTest hook failed", ex);
                }
            }
        }
    }

    public async ValueTask<List<Exception>> ExecuteAfterTestHooksAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();
        var testClassType = test.Metadata.TestClassType;

        // Execute After(Test) hooks first (specific hooks run before global hooks for cleanup)
        var afterTestHooks = await _hookCollectionService.CollectAfterTestHooksAsync(testClassType).ConfigureAwait(false);

        if (afterTestHooks.Count > 0)
        {
            foreach (var hook in afterTestHooks)
            {
                try
                {
                    test.Context.RestoreExecutionContext();
                    await hook(test.Context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(new AfterTestException($"After(Test) hook failed: {ex.Message}", ex));
                }
            }
        }

        // Execute AfterEvery(Test) hooks after After hooks (global test hooks run last for cleanup)
        var afterEveryTestHooks = await _hookCollectionService.CollectAfterEveryTestHooksAsync(testClassType).ConfigureAwait(false);

        if (afterEveryTestHooks.Count > 0)
        {
            foreach (var hook in afterEveryTestHooks)
            {
                try
                {
                    test.Context.RestoreExecutionContext();
                    await hook(test.Context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(new AfterTestException($"AfterEvery(Test) hook failed: {ex.Message}", ex));
                }
            }
        }

        return exceptions;
    }

    public async ValueTask ExecuteBeforeTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeTestDiscoveryHooksAsync().ConfigureAwait(false);

        if (hooks.Count == 0)
        {
            return;
        }

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.BeforeTestDiscoveryContext.RestoreExecutionContext();
                await hook(_contextProvider.BeforeTestDiscoveryContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new BeforeTestDiscoveryException("BeforeTestDiscovery hook failed", ex);
            }
        }
    }

    public async ValueTask ExecuteAfterTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterTestDiscoveryHooksAsync().ConfigureAwait(false);

        if (hooks.Count == 0)
        {
            return;
        }

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestDiscoveryContext.RestoreExecutionContext();
                await hook(_contextProvider.TestDiscoveryContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new AfterTestDiscoveryException("AfterTestDiscovery hook failed", ex);
            }
        }
    }
}
