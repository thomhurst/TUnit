using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Core.Logging;
using TUnit.Engine.Helpers;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

/// <summary>
/// Builds executable hook delegates from Sources collections with caching.
/// Reads hook metadata from Sources and compiles them into ready-to-execute Func delegates.
/// </summary>
internal sealed class HookDelegateBuilder : IHookDelegateBuilder
{
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<TestContext, CancellationToken, Task>>> _beforeTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<TestContext, CancellationToken, Task>>> _afterTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> _beforeClassHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> _afterClassHooksCache = new();
    private readonly ConcurrentDictionary<Assembly, IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> _beforeAssemblyHooksCache = new();
    private readonly ConcurrentDictionary<Assembly, IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> _afterAssemblyHooksCache = new();

    // Cache for GetGenericTypeDefinition() calls to avoid repeated reflection
    private static readonly ConcurrentDictionary<Type, Type> _genericTypeDefinitionCache = new();

    // Pre-computed global hooks (computed once at initialization)
    private IReadOnlyList<Func<TestContext, CancellationToken, Task>>? _beforeEveryTestHooks;
    private IReadOnlyList<Func<TestContext, CancellationToken, Task>>? _afterEveryTestHooks;
    private IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>? _beforeTestSessionHooks;
    private IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>? _afterTestSessionHooks;
    private IReadOnlyList<Func<BeforeTestDiscoveryContext, CancellationToken, Task>>? _beforeTestDiscoveryHooks;
    private IReadOnlyList<Func<TestDiscoveryContext, CancellationToken, Task>>? _afterTestDiscoveryHooks;
    private IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>? _beforeEveryClassHooks;
    private IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>? _afterEveryClassHooks;
    private IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>? _beforeEveryAssemblyHooks;
    private IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>? _afterEveryAssemblyHooks;

    // Cache for processed hooks to avoid re-processing event receivers
    private readonly ConcurrentDictionary<object, bool> _processedHooks = new();

    public HookDelegateBuilder(EventReceiverOrchestrator eventReceiverOrchestrator, TUnitFrameworkLogger logger)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _logger = logger;
    }

    private static Type GetCachedGenericTypeDefinition(Type type)
    {
        return _genericTypeDefinitionCache.GetOrAdd(type, t => t.GetGenericTypeDefinition());
    }

    public async ValueTask InitializeAsync()
    {
        await _logger.LogDebugAsync("Building global hook delegates...").ConfigureAwait(false);

        // Pre-compute all global hooks that don't depend on specific types/assemblies
        _beforeEveryTestHooks = await BuildGlobalBeforeEveryTestHooksAsync();
        _afterEveryTestHooks = await BuildGlobalAfterEveryTestHooksAsync();
        _beforeTestSessionHooks = await BuildGlobalBeforeTestSessionHooksAsync();
        _afterTestSessionHooks = await BuildGlobalAfterTestSessionHooksAsync();
        _beforeTestDiscoveryHooks = await BuildGlobalBeforeTestDiscoveryHooksAsync();
        _afterTestDiscoveryHooks = await BuildGlobalAfterTestDiscoveryHooksAsync();
        _beforeEveryClassHooks = await BuildGlobalBeforeEveryClassHooksAsync();
        _afterEveryClassHooks = await BuildGlobalAfterEveryClassHooksAsync();
        _beforeEveryAssemblyHooks = await BuildGlobalBeforeEveryAssemblyHooksAsync();
        _afterEveryAssemblyHooks = await BuildGlobalAfterEveryAssemblyHooksAsync();

        var totalHooks = _beforeEveryTestHooks.Count + _afterEveryTestHooks.Count +
                         _beforeTestSessionHooks.Count + _afterTestSessionHooks.Count +
                         _beforeTestDiscoveryHooks.Count + _afterTestDiscoveryHooks.Count +
                         _beforeEveryClassHooks.Count + _afterEveryClassHooks.Count +
                         _beforeEveryAssemblyHooks.Count + _afterEveryAssemblyHooks.Count;

        await _logger.LogDebugAsync($"Built {totalHooks} global hook delegates").ConfigureAwait(false);
    }

    /// <summary>
    /// Generic helper to build global hooks from Sources collections.
    /// Eliminates duplication across all BuildGlobalXXXHooksAsync methods.
    /// </summary>
    private async Task<IReadOnlyList<Func<TContext, CancellationToken, Task>>> BuildGlobalHooksAsync<THookMethod, TContext>(
        IEnumerable<THookMethod> sourceHooks,
        Func<THookMethod, Task<Func<TContext, CancellationToken, Task>>> createDelegate,
        string hookTypeName)
        where THookMethod : HookMethod
    {
        // Pre-size list if possible for better performance
        var capacity = sourceHooks is ICollection<THookMethod> coll ? coll.Count : 0;
        var hooks = new List<(int order, int registrationIndex, Func<TContext, CancellationToken, Task> hook)>(capacity);

        foreach (var hook in sourceHooks)
        {
            await _logger.LogDebugAsync($"Creating delegate for {hookTypeName} hook: {hook.Name}").ConfigureAwait(false);
            var hookFunc = await createDelegate(hook);
            hooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        if (hooks.Count > 0)
        {
            await _logger.LogDebugAsync($"Built {hooks.Count} {hookTypeName} hook delegate(s)").ConfigureAwait(false);
        }

        return hooks
            .OrderBy(static h => h.order)
            .ThenBy(static h => h.registrationIndex)
            .Select(static h => h.hook)
            .ToList();
    }

    private Task<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> BuildGlobalBeforeEveryTestHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeEveryTestHooks, CreateStaticHookDelegateAsync, "BeforeEveryTest");

    private Task<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> BuildGlobalAfterEveryTestHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterEveryTestHooks, CreateStaticHookDelegateAsync, "AfterEveryTest");

    private Task<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>> BuildGlobalBeforeTestSessionHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeTestSessionHooks, CreateTestSessionHookDelegateAsync, "BeforeTestSession");

    private Task<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>> BuildGlobalAfterTestSessionHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterTestSessionHooks, CreateTestSessionHookDelegateAsync, "AfterTestSession");

    private Task<IReadOnlyList<Func<BeforeTestDiscoveryContext, CancellationToken, Task>>> BuildGlobalBeforeTestDiscoveryHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeTestDiscoveryHooks, CreateBeforeTestDiscoveryHookDelegateAsync, "BeforeTestDiscovery");

    private Task<IReadOnlyList<Func<TestDiscoveryContext, CancellationToken, Task>>> BuildGlobalAfterTestDiscoveryHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterTestDiscoveryHooks, CreateTestDiscoveryHookDelegateAsync, "AfterTestDiscovery");

    private Task<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> BuildGlobalBeforeEveryClassHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeEveryClassHooks, CreateClassHookDelegateAsync, "BeforeEveryClass");

    private Task<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> BuildGlobalAfterEveryClassHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterEveryClassHooks, CreateClassHookDelegateAsync, "AfterEveryClass");

    private Task<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> BuildGlobalBeforeEveryAssemblyHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeEveryAssemblyHooks, CreateAssemblyHookDelegateAsync, "BeforeEveryAssembly");

    private Task<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> BuildGlobalAfterEveryAssemblyHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterEveryAssemblyHooks, CreateAssemblyHookDelegateAsync, "AfterEveryAssembly");

    private static void SortAndAddHooks<TDelegate>(
        List<Func<TestContext, CancellationToken, Task>> target,
        List<(int order, int registrationIndex, TDelegate hook)> hooks)
        where TDelegate : Delegate
    {
        hooks.Sort((a, b) => a.order != b.order
            ? a.order.CompareTo(b.order)
            : a.registrationIndex.CompareTo(b.registrationIndex));

        foreach (var (_, _, hook) in hooks)
        {
            target.Add((Func<TestContext, CancellationToken, Task>)(object)hook);
        }
    }

    private static void SortAndAddClassHooks(
        List<Func<ClassHookContext, CancellationToken, Task>> target,
        List<(int order, int registrationIndex, Func<ClassHookContext, CancellationToken, Task> hook)> hooks)
    {
        hooks.Sort((a, b) => a.order != b.order
            ? a.order.CompareTo(b.order)
            : a.registrationIndex.CompareTo(b.registrationIndex));

        foreach (var (_, _, hook) in hooks)
        {
            target.Add(hook);
        }
    }

    private async Task ProcessHookRegistrationAsync(HookMethod hookMethod, CancellationToken cancellationToken = default)
    {
        // Only process each hook once
        if (!_processedHooks.TryAdd(hookMethod, true))
        {
            return;
        }

        try
        {
            var context = new HookRegisteredContext(hookMethod);
            await _eventReceiverOrchestrator.InvokeHookRegistrationEventReceiversAsync(context, cancellationToken);
        }
        catch (Exception)
        {
            // Ignore errors during hook registration event processing to avoid breaking hook execution
            // The EventReceiverOrchestrator already logs errors internally
        }
    }

    public async ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectBeforeTestHooksAsync(Type testClassType)
    {
        if (_beforeTestHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildBeforeTestHooksAsync(testClassType);
        _beforeTestHooksCache.TryAdd(testClassType, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> BuildBeforeTestHooksAsync(Type type)
    {
        var hooksByType = new List<(Type type, List<(int order, int registrationIndex, Func<TestContext, CancellationToken, Task> hook)> hooks)>();

        // Collect hooks for each type in the hierarchy
        var currentType = type;
        while (currentType != null)
        {
            var typeHooks = new List<(int order, int registrationIndex, Func<TestContext, CancellationToken, Task> hook)>();

            if (Sources.BeforeTestHooks.TryGetValue(currentType, out var sourceHooks))
            {
                foreach (var hook in sourceHooks)
                {
                    var hookFunc = await CreateInstanceHookDelegateAsync(hook);
                    typeHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
                }
            }

            // Also check the open generic type definition for generic types
            if (currentType is { IsGenericType: true, IsGenericTypeDefinition: false })
            {
                var openGenericType = GetCachedGenericTypeDefinition(currentType);
                if (Sources.BeforeTestHooks.TryGetValue(openGenericType, out var openTypeHooks))
                {
                    foreach (var hook in openTypeHooks)
                    {
                        var hookFunc = await CreateInstanceHookDelegateAsync(hook);
                        typeHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
                    }
                }
            }

            if (typeHooks.Count > 0)
            {
                hooksByType.Add((currentType, typeHooks));
            }

            currentType = currentType.BaseType;
        }

        // For Before hooks: base class hooks run first
        // Reverse the list since we collected from derived to base
        hooksByType.Reverse();

        var finalHooks = new List<Func<TestContext, CancellationToken, Task>>();
        foreach (var (_, typeHooks) in hooksByType)
        {
            // Within each type level, sort by Order then by RegistrationIndex
            SortAndAddHooks(finalHooks, typeHooks);
        }

        return finalHooks;
    }

    public async ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterTestHooksAsync(Type testClassType)
    {
        if (_afterTestHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildAfterTestHooksAsync(testClassType);
        _afterTestHooksCache.TryAdd(testClassType, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> BuildAfterTestHooksAsync(Type type)
    {
        var hooksByType = new List<(Type type, List<(int order, int registrationIndex, Func<TestContext, CancellationToken, Task> hook)> hooks)>();

        // Collect hooks for each type in the hierarchy
        var currentType = type;
        while (currentType != null)
        {
            var typeHooks = new List<(int order, int registrationIndex, Func<TestContext, CancellationToken, Task> hook)>();

            if (Sources.AfterTestHooks.TryGetValue(currentType, out var sourceHooks))
            {
                foreach (var hook in sourceHooks)
                {
                    var hookFunc = await CreateInstanceHookDelegateAsync(hook);
                    typeHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
                }
            }

            // Also check the open generic type definition for generic types
            if (currentType is { IsGenericType: true, IsGenericTypeDefinition: false })
            {
                var openGenericType = GetCachedGenericTypeDefinition(currentType);
                if (Sources.AfterTestHooks.TryGetValue(openGenericType, out var openTypeHooks))
                {
                    foreach (var hook in openTypeHooks)
                    {
                        var hookFunc = await CreateInstanceHookDelegateAsync(hook);
                        typeHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
                    }
                }
            }

            if (typeHooks.Count > 0)
            {
                hooksByType.Add((currentType, typeHooks));
            }

            currentType = currentType.BaseType;
        }

        // For After hooks: derived class hooks run first
        // No need to reverse since we collected from derived to base

        var finalHooks = new List<Func<TestContext, CancellationToken, Task>>();
        foreach (var (_, typeHooks) in hooksByType)
        {
            // Within each type level, sort by Order then by RegistrationIndex
            SortAndAddHooks(finalHooks, typeHooks);
        }

        return finalHooks;
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectBeforeEveryTestHooksAsync(Type testClassType)
    {
        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(_beforeEveryTestHooks ?? []);
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterEveryTestHooksAsync(Type testClassType)
    {
        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(_afterEveryTestHooks ?? []);
    }

    public async ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectBeforeClassHooksAsync(Type testClassType)
    {
        if (_beforeClassHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildBeforeClassHooksAsync(testClassType);
        _beforeClassHooksCache.TryAdd(testClassType, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> BuildBeforeClassHooksAsync(Type type)
    {
        var hooksByType = new List<(Type type, List<(int order, int registrationIndex, Func<ClassHookContext, CancellationToken, Task> hook)> hooks)>();

        // Collect hooks for each type in the hierarchy
        var currentType = type;
        while (currentType != null)
        {
            var typeHooks = new List<(int order, int registrationIndex, Func<ClassHookContext, CancellationToken, Task> hook)>();

            if (Sources.BeforeClassHooks.TryGetValue(currentType, out var sourceHooks))
            {
                foreach (var hook in sourceHooks)
                {
                    var hookFunc = await CreateClassHookDelegateAsync(hook);
                    typeHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
                }
            }

            // Also check the open generic type definition for generic types
            if (currentType is { IsGenericType: true, IsGenericTypeDefinition: false })
            {
                var openGenericType = GetCachedGenericTypeDefinition(currentType);
                if (Sources.BeforeClassHooks.TryGetValue(openGenericType, out var openTypeHooks))
                {
                    foreach (var hook in openTypeHooks)
                    {
                        var hookFunc = await CreateClassHookDelegateAsync(hook);
                        typeHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
                    }
                }
            }

            if (typeHooks.Count > 0)
            {
                hooksByType.Add((currentType, typeHooks));
            }

            currentType = currentType.BaseType;
        }

        hooksByType.Reverse();

        var finalHooks = new List<Func<ClassHookContext, CancellationToken, Task>>();
        foreach (var (_, typeHooks) in hooksByType)
        {
            SortAndAddClassHooks(finalHooks, typeHooks);
        }

        return finalHooks;
    }

    public async ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectAfterClassHooksAsync(Type testClassType)
    {
        if (_afterClassHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildAfterClassHooksAsync(testClassType);
        _afterClassHooksCache.TryAdd(testClassType, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> BuildAfterClassHooksAsync(Type type)
    {
        var hooksByType = new List<(Type type, List<(int order, int registrationIndex, Func<ClassHookContext, CancellationToken, Task> hook)> hooks)>();

        // Collect hooks for each type in the hierarchy
        var currentType = type;
        while (currentType != null)
        {
            var typeHooks = new List<(int order, int registrationIndex, Func<ClassHookContext, CancellationToken, Task> hook)>();

            if (Sources.AfterClassHooks.TryGetValue(currentType, out var sourceHooks))
            {
                foreach (var hook in sourceHooks)
                {
                    var hookFunc = await CreateClassHookDelegateAsync(hook);
                    typeHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
                }
            }

            // Also check the open generic type definition for generic types
            if (currentType is { IsGenericType: true, IsGenericTypeDefinition: false })
            {
                var openGenericType = GetCachedGenericTypeDefinition(currentType);
                if (Sources.AfterClassHooks.TryGetValue(openGenericType, out var openTypeHooks))
                {
                    foreach (var hook in openTypeHooks)
                    {
                        var hookFunc = await CreateClassHookDelegateAsync(hook);
                        typeHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
                    }
                }
            }

            if (typeHooks.Count > 0)
            {
                hooksByType.Add((currentType, typeHooks));
            }

            currentType = currentType.BaseType;
        }

        var finalHooks = new List<Func<ClassHookContext, CancellationToken, Task>>();
        foreach (var (_, typeHooks) in hooksByType)
        {
            SortAndAddClassHooks(finalHooks, typeHooks);
        }

        return finalHooks;
    }

    public async ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectBeforeAssemblyHooksAsync(Assembly assembly)
    {
        if (_beforeAssemblyHooksCache.TryGetValue(assembly, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildBeforeAssemblyHooksAsync(assembly);
        _beforeAssemblyHooksCache.TryAdd(assembly, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> BuildBeforeAssemblyHooksAsync(Assembly assembly)
    {
        if (!Sources.BeforeAssemblyHooks.TryGetValue(assembly, out var assemblyHooks))
        {
            return [];
        }

        var allHooks = new List<(int order, Func<AssemblyHookContext, CancellationToken, Task> hook)>(assemblyHooks.Count);

        foreach (var hook in assemblyHooks)
        {
            var hookFunc = await CreateAssemblyHookDelegateAsync(hook);
            allHooks.Add((hook.Order, hookFunc));
        }

        return allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();
    }

    public async ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectAfterAssemblyHooksAsync(Assembly assembly)
    {
        if (_afterAssemblyHooksCache.TryGetValue(assembly, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildAfterAssemblyHooksAsync(assembly);
        _afterAssemblyHooksCache.TryAdd(assembly, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> BuildAfterAssemblyHooksAsync(Assembly assembly)
    {
        if (!Sources.AfterAssemblyHooks.TryGetValue(assembly, out var assemblyHooks))
        {
            return [];
        }

        var allHooks = new List<(int order, Func<AssemblyHookContext, CancellationToken, Task> hook)>(assemblyHooks.Count);

        foreach (var hook in assemblyHooks)
        {
            var hookFunc = await CreateAssemblyHookDelegateAsync(hook);
            allHooks.Add((hook.Order, hookFunc));
        }

        return allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();
    }

    public ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>> CollectBeforeTestSessionHooksAsync()
    {
        return new ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>>(_beforeTestSessionHooks ?? []);
    }

    public ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>> CollectAfterTestSessionHooksAsync()
    {
        return new ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>>(_afterTestSessionHooks ?? []);
    }

    public ValueTask<IReadOnlyList<Func<BeforeTestDiscoveryContext, CancellationToken, Task>>> CollectBeforeTestDiscoveryHooksAsync()
    {
        return new ValueTask<IReadOnlyList<Func<BeforeTestDiscoveryContext, CancellationToken, Task>>>(_beforeTestDiscoveryHooks ?? []);
    }

    public ValueTask<IReadOnlyList<Func<TestDiscoveryContext, CancellationToken, Task>>> CollectAfterTestDiscoveryHooksAsync()
    {
        return new ValueTask<IReadOnlyList<Func<TestDiscoveryContext, CancellationToken, Task>>>(_afterTestDiscoveryHooks ?? []);
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectBeforeEveryClassHooksAsync()
    {
        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(_beforeEveryClassHooks ?? []);
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectAfterEveryClassHooksAsync()
    {
        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(_afterEveryClassHooks ?? []);
    }

    public ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectBeforeEveryAssemblyHooksAsync()
    {
        return new ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>>(_beforeEveryAssemblyHooks ?? []);
    }

    public ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectAfterEveryAssemblyHooksAsync()
    {
        return new ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>>(_afterEveryAssemblyHooks ?? []);
    }

    private async Task<Func<TestContext, CancellationToken, Task>> CreateInstanceHookDelegateAsync(InstanceHookMethod hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return async (context, cancellationToken) =>
        {
            // Check at EXECUTION time if a custom executor should be used
            if (context.CustomHookExecutor != null)
            {
                // BYPASS the hook's default executor and call the custom executor directly
                var customExecutor = context.CustomHookExecutor;

                // Skip skipped test instances
                if (context.Metadata.TestDetails.ClassInstance is SkippedTestInstance)
                {
                    return;
                }

                if (context.Metadata.TestDetails.ClassInstance is PlaceholderInstance)
                {
                    throw new InvalidOperationException($"Cannot execute instance hook {hook.Name} because the test instance has not been created yet. This is likely a framework bug.");
                }

                await customExecutor.ExecuteBeforeTestHook(
                    hook.MethodInfo,
                    context,
                    () => hook.Body!.Invoke(context.Metadata.TestDetails.ClassInstance, context, cancellationToken)
                );
            }
            else
            {
                // No custom executor, use normal execution path
                var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                    (ctx, ct) => hook.ExecuteAsync(ctx, ct),
                    context,
                    hook.Timeout,
                    hook.Name,
                    cancellationToken);

                await timeoutAction();
            }
        };
    }

    private async Task<Func<TestContext, CancellationToken, Task>> CreateStaticHookDelegateAsync(StaticHookMethod<TestContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return (context, cancellationToken) => HookTimeoutHelper.CreateTimeoutHookAction(
            hook,
            context,
            cancellationToken);
    }

    private static Func<ClassHookContext, CancellationToken, Task> CreateClassHookDelegate(StaticHookMethod<ClassHookContext> hook)
    {
        return (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        };
    }

    private async Task<Func<ClassHookContext, CancellationToken, Task>> CreateClassHookDelegateAsync(StaticHookMethod<ClassHookContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        };
    }

    private static Func<AssemblyHookContext, CancellationToken, Task> CreateAssemblyHookDelegate(StaticHookMethod<AssemblyHookContext> hook)
    {
        return (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        };
    }

    private async Task<Func<AssemblyHookContext, CancellationToken, Task>> CreateAssemblyHookDelegateAsync(StaticHookMethod<AssemblyHookContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        };
    }

    private static Func<TestSessionContext, CancellationToken, Task> CreateTestSessionHookDelegate(StaticHookMethod<TestSessionContext> hook)
    {
        return (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        };
    }

    private async Task<Func<TestSessionContext, CancellationToken, Task>> CreateTestSessionHookDelegateAsync(StaticHookMethod<TestSessionContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        };
    }

    private static Func<BeforeTestDiscoveryContext, CancellationToken, Task> CreateBeforeTestDiscoveryHookDelegate(StaticHookMethod<BeforeTestDiscoveryContext> hook)
    {
        return (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        };
    }

    private async Task<Func<BeforeTestDiscoveryContext, CancellationToken, Task>> CreateBeforeTestDiscoveryHookDelegateAsync(StaticHookMethod<BeforeTestDiscoveryContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        };
    }

    private static Func<TestDiscoveryContext, CancellationToken, Task> CreateTestDiscoveryHookDelegate(StaticHookMethod<TestDiscoveryContext> hook)
    {
        return (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        };
    }

    private async Task<Func<TestDiscoveryContext, CancellationToken, Task>> CreateTestDiscoveryHookDelegateAsync(StaticHookMethod<TestDiscoveryContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        };
    }
}
