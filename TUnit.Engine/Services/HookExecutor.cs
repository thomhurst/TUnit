using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
    private readonly IHookDelegateBuilder _hookCollectionService;
    private readonly IContextProvider _contextProvider;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;

    public HookExecutor(
        IHookDelegateBuilder hookCollectionService,
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

        if (hooks.Count > 0)
        {
            foreach (var hook in hooks)
            {
                try
                {
                    _contextProvider.TestSessionContext.RestoreExecutionContext();
                    await ExecuteHookWithActivityAsync(hook, _contextProvider.TestSessionContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is SkipTestException)
                    {
                        throw;
                    }

                    if (ex.InnerException is SkipTestException skipEx)
                    {
                        ExceptionDispatchInfo.Capture(skipEx).Throw();
                    }

                    throw new BeforeTestSessionException($"BeforeTestSession hook failed: {ex.Message}", ex);
                }
            }
        }

        // Try to start the session activity now.  When the user sets up their
        // TracerProvider in Before(TestSession), this is the first opportunity
        // where HasListeners() returns true.  When they set it up earlier (e.g.
        // in Before(TestDiscovery)), the activity was already started by
        // TryStartSessionActivity() before discovery — this call is a no-op.
#if NET
        TryStartSessionActivity();
#endif
    }

    public async ValueTask<List<Exception>> ExecuteAfterTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        // Stop the session activity BEFORE hooks run, because user hooks
        // typically dispose the TracerProvider / ActivityListener. If we
        // stopped the activity after hooks, the exporter would already be
        // gone and the root span would never be exported.
#if NET
        var hasTestFailures = _contextProvider.TestSessionContext.AllTests
            .Any(t => t.Result is { State: TestState.Failed or TestState.Timeout or TestState.Cancelled });
        FinishSessionActivity(hasErrors: hasTestFailures);
#endif

        var hooks = await _hookCollectionService.CollectAfterTestSessionHooksAsync().ConfigureAwait(false);

        if (hooks.Count == 0)
        {
            return [];
        }

        // Defer exception list allocation until actually needed
        List<Exception>? exceptions = null;

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestSessionContext.RestoreExecutionContext();
                await ExecuteHookWithActivityAsync(hook, _contextProvider.TestSessionContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Collect hook exceptions instead of throwing immediately
                // This allows all hooks to run even if some fail
                exceptions ??= [];
                exceptions.Add(new AfterTestSessionException($"AfterTestSession hook failed: {ex.Message}", ex));
            }
        }

        return exceptions ?? [];
    }

#if NET
    /// <summary>
    /// Lazily starts the session activity once an ActivityListener is registered,
    /// so discovery and execution spans can parent under it.
    /// </summary>
    internal void TryStartSessionActivity()
    {
        var sessionContext = _contextProvider.TestSessionContext;

        if (sessionContext.Activity is not null)
        {
            return;
        }

        if (TUnitActivitySource.Source.HasListeners())
        {
            sessionContext.Activity = TUnitActivitySource.StartActivity(
                TUnitActivitySource.SpanTestSession,
                System.Diagnostics.ActivityKind.Internal,
                default,
                [
                    new("tunit.session.id", sessionContext.Id),
                    new("tunit.filter", sessionContext.TestFilter)
                ]);
        }
    }

    private void FinishSessionActivity(bool hasErrors)
    {
        var sessionContext = _contextProvider.TestSessionContext;
        var activity = sessionContext.Activity;

        if (activity is null)
        {
            return;
        }

        activity.SetTag("tunit.test.count", sessionContext.AllTests.Count);

        if (hasErrors)
        {
            activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
        }

        TUnitActivitySource.StopActivity(activity);
        sessionContext.Activity = null;
    }
#endif

    public async ValueTask ExecuteBeforeAssemblyHooksAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var assemblyContext = _contextProvider.GetOrCreateAssemblyContext(assembly);

#if NET
        if (TUnitActivitySource.Source.HasListeners())
        {
            var sessionActivity = _contextProvider.TestSessionContext.Activity;
            assemblyContext.Activity = TUnitActivitySource.StartActivity(
                TUnitActivitySource.SpanTestAssembly,
                System.Diagnostics.ActivityKind.Internal,
                sessionActivity?.Context ?? default,
                [
                    new("tunit.assembly.name", assembly.GetName().Name)
                ]);
        }
#endif

        // Execute BeforeEvery(Assembly) hooks first (global hooks run before specific hooks)
        var beforeEveryAssemblyHooks = await _hookCollectionService.CollectBeforeEveryAssemblyHooksAsync().ConfigureAwait(false);

        if (beforeEveryAssemblyHooks.Count > 0)
        {
            foreach (var hook in beforeEveryAssemblyHooks)
            {
                try
                {
                    assemblyContext.RestoreExecutionContext();
                    await ExecuteHookWithActivityAsync(hook, assemblyContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is SkipTestException)
                    {
                        throw;
                    }

                    if (ex.InnerException is SkipTestException skipEx)
                    {
                        ExceptionDispatchInfo.Capture(skipEx).Throw();
                    }

                    throw new BeforeAssemblyException($"BeforeEveryAssembly hook failed: {ex.Message}", ex);
                }
            }
        }

        // Execute Before(Assembly) hooks after BeforeEvery hooks
        var hooks = await _hookCollectionService.CollectBeforeAssemblyHooksAsync(assembly).ConfigureAwait(false);

        if (hooks.Count > 0)
        {
            foreach (var hook in hooks)
            {
                try
                {
                    assemblyContext.RestoreExecutionContext();
                    await ExecuteHookWithActivityAsync(hook, assemblyContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is SkipTestException)
                    {
                        throw;
                    }

                    if (ex.InnerException is SkipTestException skipEx)
                    {
                        ExceptionDispatchInfo.Capture(skipEx).Throw();
                    }

                    throw new BeforeAssemblyException($"BeforeAssembly hook failed: {ex.Message}", ex);
                }
            }
        }
    }

    public async ValueTask<List<Exception>> ExecuteAfterAssemblyHooksAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var afterAssemblyContext = _contextProvider.GetOrCreateAssemblyContext(assembly);

        // Defer exception list allocation until actually needed
        List<Exception>? exceptions = null;

        // Execute After(Assembly) hooks first (specific hooks run before global hooks for cleanup)
        var hooks = await _hookCollectionService.CollectAfterAssemblyHooksAsync(assembly).ConfigureAwait(false);

        if (hooks.Count > 0)
        {
            foreach (var hook in hooks)
            {
                try
                {
                    afterAssemblyContext.RestoreExecutionContext();
                    await ExecuteHookWithActivityAsync(hook, afterAssemblyContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Collect hook exceptions instead of throwing immediately
                    // This allows all hooks to run even if some fail
                    exceptions ??= [];
                    exceptions.Add(new AfterAssemblyException($"AfterAssembly hook failed: {ex.Message}", ex));
                }
            }
        }

        // Execute AfterEvery(Assembly) hooks after After hooks (global hooks run last for cleanup)
        var afterEveryAssemblyHooks = await _hookCollectionService.CollectAfterEveryAssemblyHooksAsync().ConfigureAwait(false);

        if (afterEveryAssemblyHooks.Count > 0)
        {
            foreach (var hook in afterEveryAssemblyHooks)
            {
                try
                {
                    afterAssemblyContext.RestoreExecutionContext();
                    await ExecuteHookWithActivityAsync(hook, afterAssemblyContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(new AfterAssemblyException($"AfterEveryAssembly hook failed: {ex.Message}", ex));
                }
            }
        }

#if NET
        FinishAssemblyActivity(assembly, hasErrors: exceptions is { Count: > 0 });
#endif

        return exceptions ?? [];
    }

#if NET
    private void FinishAssemblyActivity(Assembly assembly, bool hasErrors)
    {
        var assemblyContext = _contextProvider.GetOrCreateAssemblyContext(assembly);
        var activity = assemblyContext.Activity;

        if (activity is null)
        {
            return;
        }

        activity.SetTag("tunit.test.count", assemblyContext.TestCount);

        if (hasErrors)
        {
            activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
        }

        TUnitActivitySource.StopActivity(activity);
        assemblyContext.Activity = null;
    }
#endif

    public async ValueTask ExecuteBeforeClassHooksAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, CancellationToken cancellationToken)
    {
        var classContext = _contextProvider.GetOrCreateClassContext(testClass);

#if NET
        if (TUnitActivitySource.Source.HasListeners())
        {
            var assemblyActivity = classContext.AssemblyContext.Activity;
            classContext.Activity = TUnitActivitySource.StartActivity(
                TUnitActivitySource.SpanTestSuite,
                System.Diagnostics.ActivityKind.Internal,
                assemblyActivity?.Context ?? default,
                [
                    new("test.suite.name", testClass.Name),
                    new("tunit.class.namespace", testClass.Namespace)
                ]);
        }
#endif

        // Execute BeforeEvery(Class) hooks first (global hooks run before specific hooks)
        var beforeEveryClassHooks = await _hookCollectionService.CollectBeforeEveryClassHooksAsync().ConfigureAwait(false);

        if (beforeEveryClassHooks.Count > 0)
        {
            foreach (var hook in beforeEveryClassHooks)
            {
                try
                {
                    classContext.RestoreExecutionContext();
                    await ExecuteHookWithActivityAsync(hook, classContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is SkipTestException)
                    {
                        throw;
                    }

                    if (ex.InnerException is SkipTestException skipEx)
                    {
                        ExceptionDispatchInfo.Capture(skipEx).Throw();
                    }

                    throw new BeforeClassException($"BeforeEveryClass hook failed: {ex.Message}", ex);
                }
            }
        }

        // Execute Before(Class) hooks after BeforeEvery hooks
        var hooks = await _hookCollectionService.CollectBeforeClassHooksAsync(testClass).ConfigureAwait(false);

        if (hooks.Count > 0)
        {
            foreach (var hook in hooks)
            {
                try
                {
                    classContext.RestoreExecutionContext();
                    await ExecuteHookWithActivityAsync(hook, classContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is SkipTestException)
                    {
                        throw;
                    }

                    if (ex.InnerException is SkipTestException skipEx)
                    {
                        ExceptionDispatchInfo.Capture(skipEx).Throw();
                    }

                    throw new BeforeClassException($"BeforeClass hook failed: {ex.Message}", ex);
                }
            }
        }
    }

    public async ValueTask<List<Exception>> ExecuteAfterClassHooksAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, CancellationToken cancellationToken)
    {
        var afterClassContext = _contextProvider.GetOrCreateClassContext(testClass);

        // Defer exception list allocation until actually needed
        List<Exception>? exceptions = null;

        // Execute After(Class) hooks first (specific hooks run before global hooks for cleanup)
        var hooks = await _hookCollectionService.CollectAfterClassHooksAsync(testClass).ConfigureAwait(false);

        if (hooks.Count > 0)
        {
            foreach (var hook in hooks)
            {
                try
                {
                    afterClassContext.RestoreExecutionContext();
                    await ExecuteHookWithActivityAsync(hook, afterClassContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Collect hook exceptions instead of throwing immediately
                    // This allows all hooks to run even if some fail
                    exceptions ??= [];
                    exceptions.Add(new AfterClassException($"AfterClass hook failed: {ex.Message}", ex));
                }
            }
        }

        // Execute AfterEvery(Class) hooks after After hooks (global hooks run last for cleanup)
        var afterEveryClassHooks = await _hookCollectionService.CollectAfterEveryClassHooksAsync().ConfigureAwait(false);

        if (afterEveryClassHooks.Count > 0)
        {
            foreach (var hook in afterEveryClassHooks)
            {
                try
                {
                    afterClassContext.RestoreExecutionContext();
                    await ExecuteHookWithActivityAsync(hook, afterClassContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(new AfterClassException($"AfterEveryClass hook failed: {ex.Message}", ex));
                }
            }
        }

#if NET
        FinishClassActivity(testClass, hasErrors: exceptions is { Count: > 0 });
#endif

        return exceptions ?? [];
    }

#if NET
    private void FinishClassActivity(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, bool hasErrors)
    {
        var classContext = _contextProvider.GetOrCreateClassContext(testClass);
        var activity = classContext.Activity;

        if (activity is null)
        {
            return;
        }

        activity.SetTag("tunit.test.count", classContext.TestCount);

        if (hasErrors)
        {
            activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
        }

        TUnitActivitySource.StopActivity(activity);
        classContext.Activity = null;
    }
#endif

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
                    await ExecuteHookWithActivityAsync(hook, test.Context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is SkipTestException)
                    {
                        throw;
                    }

                    if (ex.InnerException is SkipTestException skipEx)
                    {
                        ExceptionDispatchInfo.Capture(skipEx).Throw();
                    }

                    throw new BeforeTestException($"BeforeEveryTest hook failed: {ex.Message}", ex);
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
                    await ExecuteHookWithActivityAsync(hook, test.Context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is SkipTestException)
                    {
                        throw;
                    }

                    if (ex.InnerException is SkipTestException skipEx)
                    {
                        ExceptionDispatchInfo.Capture(skipEx).Throw();
                    }

                    throw new BeforeTestException($"BeforeTest hook failed: {ex.Message}", ex);
                }
            }
        }
    }

    public async ValueTask<IReadOnlyList<Exception>> ExecuteAfterTestHooksAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        // Defer exception list allocation until actually needed
        List<Exception>? exceptions = null;
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
                    await ExecuteHookWithActivityAsync(hook, test.Context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
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
                    await ExecuteHookWithActivityAsync(hook, test.Context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(new AfterTestException($"AfterEvery(Test) hook failed: {ex.Message}", ex));
                }
            }
        }

        return exceptions ?? [];
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
                await ExecuteHookWithActivityAsync(hook, _contextProvider.BeforeTestDiscoveryContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new BeforeTestDiscoveryException($"BeforeTestDiscovery hook failed: {ex.Message}", ex);
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
                await ExecuteHookWithActivityAsync(hook, _contextProvider.TestDiscoveryContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new AfterTestDiscoveryException($"AfterTestDiscovery hook failed: {ex.Message}", ex);
            }
        }
    }

#if NET
    private static async ValueTask ExecuteHookWithActivityAsync<TContext>(NamedHookDelegate<TContext> hook, TContext context, CancellationToken cancellationToken)
        where TContext : Context
    {
        System.Diagnostics.Activity? hookActivity = null;

        if (TUnitActivitySource.Source.HasListeners())
        {
            hookActivity = TUnitActivitySource.StartActivity(
                hook.ActivityName,
                System.Diagnostics.ActivityKind.Internal,
                context.Activity?.Context ?? default);
        }

        try
        {
            await hook.Invoke(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            TUnitActivitySource.RecordException(hookActivity, ex);
            throw;
        }
        finally
        {
            TUnitActivitySource.StopActivity(hookActivity);
        }
    }
#else
    private static async ValueTask ExecuteHookWithActivityAsync<TContext>(NamedHookDelegate<TContext> hook, TContext context, CancellationToken cancellationToken)
        where TContext : Context
    {
        await hook.Invoke(context, cancellationToken).ConfigureAwait(false);
    }
#endif
}
