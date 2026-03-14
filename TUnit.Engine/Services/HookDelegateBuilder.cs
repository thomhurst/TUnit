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
    private readonly ConcurrentDictionary<Type, IReadOnlyList<NamedHookDelegate<TestContext>>> _beforeTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<NamedHookDelegate<TestContext>>> _afterTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<NamedHookDelegate<ClassHookContext>>> _beforeClassHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<NamedHookDelegate<ClassHookContext>>> _afterClassHooksCache = new();
    private readonly ConcurrentDictionary<Assembly, IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> _beforeAssemblyHooksCache = new();
    private readonly ConcurrentDictionary<Assembly, IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> _afterAssemblyHooksCache = new();

    // Cache for GetGenericTypeDefinition() calls to avoid repeated reflection
    private static readonly ConcurrentDictionary<Type, Type> _genericTypeDefinitionCache = new();

    // Pre-computed global hooks (computed once at initialization)
    private IReadOnlyList<NamedHookDelegate<TestContext>>? _beforeEveryTestHooks;
    private IReadOnlyList<NamedHookDelegate<TestContext>>? _afterEveryTestHooks;
    private IReadOnlyList<NamedHookDelegate<TestSessionContext>>? _beforeTestSessionHooks;
    private IReadOnlyList<NamedHookDelegate<TestSessionContext>>? _afterTestSessionHooks;
    private IReadOnlyList<NamedHookDelegate<BeforeTestDiscoveryContext>>? _beforeTestDiscoveryHooks;
    private IReadOnlyList<NamedHookDelegate<TestDiscoveryContext>>? _afterTestDiscoveryHooks;
    private IReadOnlyList<NamedHookDelegate<ClassHookContext>>? _beforeEveryClassHooks;
    private IReadOnlyList<NamedHookDelegate<ClassHookContext>>? _afterEveryClassHooks;
    private IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>? _beforeEveryAssemblyHooks;
    private IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>? _afterEveryAssemblyHooks;

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
    private async Task<IReadOnlyList<NamedHookDelegate<TContext>>> BuildGlobalHooksAsync<THookMethod, TContext>(
        IEnumerable<THookMethod> sourceHooks,
        Func<THookMethod, Task<NamedHookDelegate<TContext>>> createDelegate,
        string hookTypeName)
        where THookMethod : HookMethod
    {
        // Pre-size list if possible for better performance
        var capacity = sourceHooks is ICollection<THookMethod> coll ? coll.Count : 0;
        var hooks = new List<(int order, int registrationIndex, NamedHookDelegate<TContext> hook)>(capacity);

        foreach (var hook in sourceHooks)
        {
            await _logger.LogTraceAsync($"Creating delegate for {hookTypeName} hook: {hook.Name}").ConfigureAwait(false);
            var namedHook = await createDelegate(hook);
            hooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
        }

        if (hooks.Count > 0)
        {
            await _logger.LogTraceAsync($"Built {hooks.Count} {hookTypeName} hook delegate(s)").ConfigureAwait(false);
        }

        return hooks
            .OrderBy(static h => h.order)
            .ThenBy(static h => h.registrationIndex)
            .Select(static h => h.hook)
            .ToList();
    }

    private Task<IReadOnlyList<NamedHookDelegate<TestContext>>> BuildGlobalBeforeEveryTestHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeEveryTestHooks, CreateStaticHookDelegateAsync, "BeforeEveryTest");

    private Task<IReadOnlyList<NamedHookDelegate<TestContext>>> BuildGlobalAfterEveryTestHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterEveryTestHooks, CreateStaticHookDelegateAsync, "AfterEveryTest");

    private Task<IReadOnlyList<NamedHookDelegate<TestSessionContext>>> BuildGlobalBeforeTestSessionHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeTestSessionHooks, CreateTestSessionHookDelegateAsync, "BeforeTestSession");

    private Task<IReadOnlyList<NamedHookDelegate<TestSessionContext>>> BuildGlobalAfterTestSessionHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterTestSessionHooks, CreateTestSessionHookDelegateAsync, "AfterTestSession");

    private Task<IReadOnlyList<NamedHookDelegate<BeforeTestDiscoveryContext>>> BuildGlobalBeforeTestDiscoveryHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeTestDiscoveryHooks, CreateBeforeTestDiscoveryHookDelegateAsync, "BeforeTestDiscovery");

    private Task<IReadOnlyList<NamedHookDelegate<TestDiscoveryContext>>> BuildGlobalAfterTestDiscoveryHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterTestDiscoveryHooks, CreateTestDiscoveryHookDelegateAsync, "AfterTestDiscovery");

    private Task<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> BuildGlobalBeforeEveryClassHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeEveryClassHooks, CreateClassHookDelegateAsync, "BeforeEveryClass");

    private Task<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> BuildGlobalAfterEveryClassHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterEveryClassHooks, CreateClassHookDelegateAsync, "AfterEveryClass");

    private Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> BuildGlobalBeforeEveryAssemblyHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeEveryAssemblyHooks, CreateAssemblyHookDelegateAsync, "BeforeEveryAssembly");

    private Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> BuildGlobalAfterEveryAssemblyHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterEveryAssemblyHooks, CreateAssemblyHookDelegateAsync, "AfterEveryAssembly");

    private static void SortAndAddHooks<TDelegate>(
        List<NamedHookDelegate<TestContext>> target,
        List<(int order, int registrationIndex, TDelegate hook)> hooks)
        where TDelegate : struct
    {
        hooks.Sort((a, b) => a.order != b.order
            ? a.order.CompareTo(b.order)
            : a.registrationIndex.CompareTo(b.registrationIndex));

        foreach (var (_, _, hook) in hooks)
        {
            target.Add((NamedHookDelegate<TestContext>)(object)hook);
        }
    }

    private static void SortAndAddClassHooks(
        List<NamedHookDelegate<ClassHookContext>> target,
        List<(int order, int registrationIndex, NamedHookDelegate<ClassHookContext> hook)> hooks)
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

    public async ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectBeforeTestHooksAsync(Type testClassType)
    {
        if (_beforeTestHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildBeforeTestHooksAsync(testClassType);
        _beforeTestHooksCache.TryAdd(testClassType, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<NamedHookDelegate<TestContext>>> BuildBeforeTestHooksAsync(Type type)
    {
        var hooksByType = new List<(Type type, List<(int order, int registrationIndex, NamedHookDelegate<TestContext> hook)> hooks)>();

        // Collect hooks for each type in the hierarchy
        var currentType = type;
        while (currentType != null)
        {
            var typeHooks = new List<(int order, int registrationIndex, NamedHookDelegate<TestContext> hook)>();

            if (Sources.BeforeTestHooks.TryGetValue(currentType, out var sourceHooks))
            {
                foreach (var hook in sourceHooks)
                {
                    var namedHook = await CreateInstanceHookDelegateAsync(hook);
                    typeHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
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
                        var namedHook = await CreateInstanceHookDelegateAsync(hook);
                        typeHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
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

        var finalHooks = new List<NamedHookDelegate<TestContext>>();
        foreach (var (_, typeHooks) in hooksByType)
        {
            // Within each type level, sort by Order then by RegistrationIndex
            SortAndAddHooks(finalHooks, typeHooks);
        }

        return finalHooks;
    }

    public async ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectAfterTestHooksAsync(Type testClassType)
    {
        if (_afterTestHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildAfterTestHooksAsync(testClassType);
        _afterTestHooksCache.TryAdd(testClassType, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<NamedHookDelegate<TestContext>>> BuildAfterTestHooksAsync(Type type)
    {
        var hooksByType = new List<(Type type, List<(int order, int registrationIndex, NamedHookDelegate<TestContext> hook)> hooks)>();

        // Collect hooks for each type in the hierarchy
        var currentType = type;
        while (currentType != null)
        {
            var typeHooks = new List<(int order, int registrationIndex, NamedHookDelegate<TestContext> hook)>();

            if (Sources.AfterTestHooks.TryGetValue(currentType, out var sourceHooks))
            {
                foreach (var hook in sourceHooks)
                {
                    var namedHook = await CreateInstanceHookDelegateAsync(hook);
                    typeHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
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
                        var namedHook = await CreateInstanceHookDelegateAsync(hook);
                        typeHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
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

        var finalHooks = new List<NamedHookDelegate<TestContext>>();
        foreach (var (_, typeHooks) in hooksByType)
        {
            // Within each type level, sort by Order then by RegistrationIndex
            SortAndAddHooks(finalHooks, typeHooks);
        }

        return finalHooks;
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectBeforeEveryTestHooksAsync(Type testClassType)
    {
        return new ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>>(_beforeEveryTestHooks ?? []);
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectAfterEveryTestHooksAsync(Type testClassType)
    {
        return new ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>>(_afterEveryTestHooks ?? []);
    }

    public async ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectBeforeClassHooksAsync(Type testClassType)
    {
        if (_beforeClassHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildBeforeClassHooksAsync(testClassType);
        _beforeClassHooksCache.TryAdd(testClassType, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> BuildBeforeClassHooksAsync(Type type)
    {
        var hooksByType = new List<(Type type, List<(int order, int registrationIndex, NamedHookDelegate<ClassHookContext> hook)> hooks)>();

        // Collect hooks for each type in the hierarchy
        var currentType = type;
        while (currentType != null)
        {
            var typeHooks = new List<(int order, int registrationIndex, NamedHookDelegate<ClassHookContext> hook)>();

            if (Sources.BeforeClassHooks.TryGetValue(currentType, out var sourceHooks))
            {
                foreach (var hook in sourceHooks)
                {
                    var namedHook = await CreateClassHookDelegateAsync(hook);
                    typeHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
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
                        var namedHook = await CreateClassHookDelegateAsync(hook);
                        typeHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
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

        var finalHooks = new List<NamedHookDelegate<ClassHookContext>>();
        foreach (var (_, typeHooks) in hooksByType)
        {
            SortAndAddClassHooks(finalHooks, typeHooks);
        }

        return finalHooks;
    }

    public async ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectAfterClassHooksAsync(Type testClassType)
    {
        if (_afterClassHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildAfterClassHooksAsync(testClassType);
        _afterClassHooksCache.TryAdd(testClassType, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> BuildAfterClassHooksAsync(Type type)
    {
        var hooksByType = new List<(Type type, List<(int order, int registrationIndex, NamedHookDelegate<ClassHookContext> hook)> hooks)>();

        // Collect hooks for each type in the hierarchy
        var currentType = type;
        while (currentType != null)
        {
            var typeHooks = new List<(int order, int registrationIndex, NamedHookDelegate<ClassHookContext> hook)>();

            if (Sources.AfterClassHooks.TryGetValue(currentType, out var sourceHooks))
            {
                foreach (var hook in sourceHooks)
                {
                    var namedHook = await CreateClassHookDelegateAsync(hook);
                    typeHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
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
                        var namedHook = await CreateClassHookDelegateAsync(hook);
                        typeHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
                    }
                }
            }

            if (typeHooks.Count > 0)
            {
                hooksByType.Add((currentType, typeHooks));
            }

            currentType = currentType.BaseType;
        }

        var finalHooks = new List<NamedHookDelegate<ClassHookContext>>();
        foreach (var (_, typeHooks) in hooksByType)
        {
            SortAndAddClassHooks(finalHooks, typeHooks);
        }

        return finalHooks;
    }

    public async ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectBeforeAssemblyHooksAsync(Assembly assembly)
    {
        if (_beforeAssemblyHooksCache.TryGetValue(assembly, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildBeforeAssemblyHooksAsync(assembly);
        _beforeAssemblyHooksCache.TryAdd(assembly, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> BuildBeforeAssemblyHooksAsync(Assembly assembly)
    {
        if (!Sources.BeforeAssemblyHooks.TryGetValue(assembly, out var assemblyHooks))
        {
            return [];
        }

        var allHooks = new List<(int order, NamedHookDelegate<AssemblyHookContext> hook)>(assemblyHooks.Count);

        foreach (var hook in assemblyHooks)
        {
            var namedHook = await CreateAssemblyHookDelegateAsync(hook);
            allHooks.Add((hook.Order, namedHook));
        }

        return allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();
    }

    public async ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectAfterAssemblyHooksAsync(Assembly assembly)
    {
        if (_afterAssemblyHooksCache.TryGetValue(assembly, out var cachedHooks))
        {
            return cachedHooks;
        }

        var hooks = await BuildAfterAssemblyHooksAsync(assembly);
        _afterAssemblyHooksCache.TryAdd(assembly, hooks);
        return hooks;
    }

    private async Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> BuildAfterAssemblyHooksAsync(Assembly assembly)
    {
        if (!Sources.AfterAssemblyHooks.TryGetValue(assembly, out var assemblyHooks))
        {
            return [];
        }

        var allHooks = new List<(int order, NamedHookDelegate<AssemblyHookContext> hook)>(assemblyHooks.Count);

        foreach (var hook in assemblyHooks)
        {
            var namedHook = await CreateAssemblyHookDelegateAsync(hook);
            allHooks.Add((hook.Order, namedHook));
        }

        return allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<TestSessionContext>>> CollectBeforeTestSessionHooksAsync()
    {
        return new ValueTask<IReadOnlyList<NamedHookDelegate<TestSessionContext>>>(_beforeTestSessionHooks ?? []);
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<TestSessionContext>>> CollectAfterTestSessionHooksAsync()
    {
        return new ValueTask<IReadOnlyList<NamedHookDelegate<TestSessionContext>>>(_afterTestSessionHooks ?? []);
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<BeforeTestDiscoveryContext>>> CollectBeforeTestDiscoveryHooksAsync()
    {
        return new ValueTask<IReadOnlyList<NamedHookDelegate<BeforeTestDiscoveryContext>>>(_beforeTestDiscoveryHooks ?? []);
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<TestDiscoveryContext>>> CollectAfterTestDiscoveryHooksAsync()
    {
        return new ValueTask<IReadOnlyList<NamedHookDelegate<TestDiscoveryContext>>>(_afterTestDiscoveryHooks ?? []);
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectBeforeEveryClassHooksAsync()
    {
        return new ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>>(_beforeEveryClassHooks ?? []);
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectAfterEveryClassHooksAsync()
    {
        return new ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>>(_afterEveryClassHooks ?? []);
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectBeforeEveryAssemblyHooksAsync()
    {
        return new ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>>(_beforeEveryAssemblyHooks ?? []);
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectAfterEveryAssemblyHooksAsync()
    {
        return new ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>>(_afterEveryAssemblyHooks ?? []);
    }

    private async Task<NamedHookDelegate<TestContext>> CreateInstanceHookDelegateAsync(InstanceHookMethod hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        var name = hook.Name;

        return new NamedHookDelegate<TestContext>(name, async (context, cancellationToken) =>
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
        });
    }

    private async Task<NamedHookDelegate<TestContext>> CreateStaticHookDelegateAsync(StaticHookMethod<TestContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return new NamedHookDelegate<TestContext>(hook.Name, (context, cancellationToken) => HookTimeoutHelper.CreateTimeoutHookAction(
            hook,
            context,
            cancellationToken));
    }

    private async Task<NamedHookDelegate<ClassHookContext>> CreateClassHookDelegateAsync(StaticHookMethod<ClassHookContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return new NamedHookDelegate<ClassHookContext>(hook.Name, (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        });
    }

    private async Task<NamedHookDelegate<AssemblyHookContext>> CreateAssemblyHookDelegateAsync(StaticHookMethod<AssemblyHookContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return new NamedHookDelegate<AssemblyHookContext>(hook.Name, (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        });
    }

    private async Task<NamedHookDelegate<TestSessionContext>> CreateTestSessionHookDelegateAsync(StaticHookMethod<TestSessionContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return new NamedHookDelegate<TestSessionContext>(hook.Name, (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        });
    }

    private async Task<NamedHookDelegate<BeforeTestDiscoveryContext>> CreateBeforeTestDiscoveryHookDelegateAsync(StaticHookMethod<BeforeTestDiscoveryContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return new NamedHookDelegate<BeforeTestDiscoveryContext>(hook.Name, (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        });
    }

    private async Task<NamedHookDelegate<TestDiscoveryContext>> CreateTestDiscoveryHookDelegateAsync(StaticHookMethod<TestDiscoveryContext> hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        return new NamedHookDelegate<TestDiscoveryContext>(hook.Name, (context, cancellationToken) =>
        {
            return HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
        });
    }
}
