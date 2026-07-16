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
    // Lazy<Task<...>> ensures only one thread builds the hook list per key — racing threads
    // observe the same Lazy instance via GetOrAdd and await the same underlying Task.
    // Without this wrapper, two threads racing the first test of the same class would both
    // build the full sorted hook list and the loser's result is silently discarded.
    private readonly ConcurrentDictionary<Type, Lazy<Task<IReadOnlyList<NamedHookDelegate<TestContext>>>>> _beforeTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, Lazy<Task<IReadOnlyList<NamedHookDelegate<TestContext>>>>> _afterTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, Lazy<Task<IReadOnlyList<NamedHookDelegate<ClassHookContext>>>>> _beforeClassHooksCache = new();
    private readonly ConcurrentDictionary<Type, Lazy<Task<IReadOnlyList<NamedHookDelegate<ClassHookContext>>>>> _afterClassHooksCache = new();
    private readonly ConcurrentDictionary<Assembly, Lazy<Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>>>> _beforeAssemblyHooksCache = new();
    private readonly ConcurrentDictionary<Assembly, Lazy<Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>>>> _afterAssemblyHooksCache = new();

    // Cache the build delegates so the per-test cache-hit path doesn't allocate a new
    // method-group delegate on every call (matters because Collect*HooksAsync runs per test).
    private readonly Func<Type, Task<IReadOnlyList<NamedHookDelegate<TestContext>>>> _buildBeforeTestHooks;
    private readonly Func<Type, Task<IReadOnlyList<NamedHookDelegate<TestContext>>>> _buildAfterTestHooks;
    private readonly Func<Type, Task<IReadOnlyList<NamedHookDelegate<ClassHookContext>>>> _buildBeforeClassHooks;
    private readonly Func<Type, Task<IReadOnlyList<NamedHookDelegate<ClassHookContext>>>> _buildAfterClassHooks;
    private readonly Func<Assembly, Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>>> _buildBeforeAssemblyHooks;
    private readonly Func<Assembly, Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>>> _buildAfterAssemblyHooks;

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

    // Cache for processed hooks to avoid re-processing event receivers.
    // Dedup relies on reference identity (HookMethod does not override Equals/GetHashCode);
    // safe today because Materialize() returns the same cached instance per hook.
    // If HookMethod ever overrides Equals/GetHashCode, revisit this assumption.
    private readonly ConcurrentDictionary<object, bool> _processedHooks = new();

    public HookDelegateBuilder(EventReceiverOrchestrator eventReceiverOrchestrator, TUnitFrameworkLogger logger)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _logger = logger;
        _buildBeforeTestHooks = BuildBeforeTestHooksAsync;
        _buildAfterTestHooks = BuildAfterTestHooksAsync;
        _buildBeforeClassHooks = BuildBeforeClassHooksAsync;
        _buildAfterClassHooks = BuildAfterClassHooksAsync;
        _buildBeforeAssemblyHooks = BuildBeforeAssemblyHooksAsync;
        _buildAfterAssemblyHooks = BuildAfterAssemblyHooksAsync;
    }

    private static Type GetCachedGenericTypeDefinition(Type type)
    {
        return _genericTypeDefinitionCache.GetOrAdd(type, static t => t.GetGenericTypeDefinition());
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
    /// Materializes each <see cref="LazyHookEntry{T}"/> on first access — the heavy
    /// MethodMetadata/ClassMetadata graph is constructed here rather than at module init.
    /// </summary>
    private async Task<IReadOnlyList<NamedHookDelegate<TContext>>> BuildGlobalHooksAsync<THookMethod, TContext>(
        IEnumerable<LazyHookEntry<THookMethod>> sourceHooks,
        Func<THookMethod, ValueTask<NamedHookDelegate<TContext>>> createDelegate,
        string hookTypeName)
        where THookMethod : HookMethod
    {
        // Pre-size list if possible for better performance
        var capacity = sourceHooks is ICollection<LazyHookEntry<THookMethod>> coll ? coll.Count : 0;
        var hooks = new List<(int order, int registrationIndex, NamedHookDelegate<TContext> hook)>(capacity);

        foreach (var entry in sourceHooks)
        {
            var hook = entry.Materialize();
            await _logger.LogTraceAsync($"Creating delegate for {hookTypeName} hook: {hook.Name}").ConfigureAwait(false);
            var namedHook = await createDelegate(hook);
            hooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
        }

        if (hooks.Count > 0)
        {
            await _logger.LogTraceAsync($"Built {hooks.Count} {hookTypeName} hook delegate(s)").ConfigureAwait(false);
        }

        return SortAndProject(hooks);
    }

    private static List<NamedHookDelegate<TContext>> SortAndProject<TContext>(
        List<(int order, int registrationIndex, NamedHookDelegate<TContext> hook)> hooks)
    {
        hooks.Sort(static (a, b) =>
        {
            var orderCompare = a.order.CompareTo(b.order);
            return orderCompare != 0 ? orderCompare : a.registrationIndex.CompareTo(b.registrationIndex);
        });

        var result = new List<NamedHookDelegate<TContext>>(hooks.Count);
        for (var i = 0; i < hooks.Count; i++)
        {
            result.Add(hooks[i].hook);
        }
        return result;
    }

    private Task<IReadOnlyList<NamedHookDelegate<TestContext>>> BuildGlobalBeforeEveryTestHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeEveryTestHooks, CreateStaticHookDelegateAsync, "BeforeEveryTest");

    private Task<IReadOnlyList<NamedHookDelegate<TestContext>>> BuildGlobalAfterEveryTestHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterEveryTestHooks, CreateStaticHookDelegateAsync, "AfterEveryTest");

    private Task<IReadOnlyList<NamedHookDelegate<TestSessionContext>>> BuildGlobalBeforeTestSessionHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeTestSessionHooks, CreateStaticHookDelegateAsync, "BeforeTestSession");

    private Task<IReadOnlyList<NamedHookDelegate<TestSessionContext>>> BuildGlobalAfterTestSessionHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterTestSessionHooks, CreateStaticHookDelegateAsync, "AfterTestSession");

    private Task<IReadOnlyList<NamedHookDelegate<BeforeTestDiscoveryContext>>> BuildGlobalBeforeTestDiscoveryHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeTestDiscoveryHooks, CreateStaticHookDelegateAsync, "BeforeTestDiscovery");

    private Task<IReadOnlyList<NamedHookDelegate<TestDiscoveryContext>>> BuildGlobalAfterTestDiscoveryHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterTestDiscoveryHooks, CreateStaticHookDelegateAsync, "AfterTestDiscovery");

    private Task<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> BuildGlobalBeforeEveryClassHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeEveryClassHooks, CreateStaticHookDelegateAsync, "BeforeEveryClass");

    private Task<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> BuildGlobalAfterEveryClassHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterEveryClassHooks, CreateStaticHookDelegateAsync, "AfterEveryClass");

    private Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> BuildGlobalBeforeEveryAssemblyHooksAsync()
        => BuildGlobalHooksAsync(Sources.BeforeEveryAssemblyHooks, CreateStaticHookDelegateAsync, "BeforeEveryAssembly");

    private Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> BuildGlobalAfterEveryAssemblyHooksAsync()
        => BuildGlobalHooksAsync(Sources.AfterEveryAssemblyHooks, CreateStaticHookDelegateAsync, "AfterEveryAssembly");

    private static void SortAndAddHooks<TContext>(
        List<NamedHookDelegate<TContext>> target,
        List<(int order, int registrationIndex, NamedHookDelegate<TContext> hook)> hooks)
    {
        hooks.Sort(static (a, b) => a.order != b.order
            ? a.order.CompareTo(b.order)
            : a.registrationIndex.CompareTo(b.registrationIndex));

        foreach (var (_, _, hook) in hooks)
        {
            target.Add(hook);
        }
    }

    /// <summary>
    /// Single-flight cache lookup: returns the shared <see cref="Lazy{T}"/> task for a key,
    /// creating one (per-instance) on first access. Other racing threads observe the same
    /// Lazy and await the same underlying build, avoiding duplicate work.
    /// </summary>
    private static ValueTask<IReadOnlyList<NamedHookDelegate<TContext>>> GetOrBuildHooksAsync<TKey, TContext>(
        ConcurrentDictionary<TKey, Lazy<Task<IReadOnlyList<NamedHookDelegate<TContext>>>>> cache,
        TKey key,
        Func<TKey, Task<IReadOnlyList<NamedHookDelegate<TContext>>>> build)
        where TKey : notnull
    {
        var lazy = cache.GetOrAdd(
            key,
            static (k, b) => new Lazy<Task<IReadOnlyList<NamedHookDelegate<TContext>>>>(
                () => b(k),
                LazyThreadSafetyMode.ExecutionAndPublication),
            build);
        var task = lazy.Value;
        return task.Status == TaskStatus.RanToCompletion
            ? new ValueTask<IReadOnlyList<NamedHookDelegate<TContext>>>(task.Result)
            : new ValueTask<IReadOnlyList<NamedHookDelegate<TContext>>>(task);
    }

    /// <summary>
    /// Synchronous fast-path for the executor: returns the cached hook list only if the
    /// build task has already completed successfully.
    /// </summary>
    private static bool TryGetCachedHooks<TKey, TContext>(
        ConcurrentDictionary<TKey, Lazy<Task<IReadOnlyList<NamedHookDelegate<TContext>>>>> cache,
        TKey key,
        out IReadOnlyList<NamedHookDelegate<TContext>> hooks)
        where TKey : notnull
    {
        if (cache.TryGetValue(key, out var cached)
            && cached.IsValueCreated
            && cached.Value.Status == TaskStatus.RanToCompletion)
        {
            hooks = cached.Value.Result;
            return true;
        }

        hooks = [];
        return false;
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

    public ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectBeforeTestHooksAsync(Type testClassType)
        => GetOrBuildHooksAsync(_beforeTestHooksCache, testClassType, _buildBeforeTestHooks);

    public bool TryGetCachedBeforeTestHooks(Type testClassType, out IReadOnlyList<NamedHookDelegate<TestContext>> hooks)
        => TryGetCachedHooks(_beforeTestHooksCache, testClassType, out hooks);

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
                foreach (var entry in sourceHooks)
                {
                    var hook = entry.Materialize();
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
                    foreach (var entry in openTypeHooks)
                    {
                        var hook = entry.Materialize();
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

    public ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectAfterTestHooksAsync(Type testClassType)
        => GetOrBuildHooksAsync(_afterTestHooksCache, testClassType, _buildAfterTestHooks);

    public bool TryGetCachedAfterTestHooks(Type testClassType, out IReadOnlyList<NamedHookDelegate<TestContext>> hooks)
        => TryGetCachedHooks(_afterTestHooksCache, testClassType, out hooks);

    public IReadOnlyList<NamedHookDelegate<TestContext>> GetCachedBeforeEveryTestHooks()
        => _beforeEveryTestHooks ?? [];

    public IReadOnlyList<NamedHookDelegate<TestContext>> GetCachedAfterEveryTestHooks()
        => _afterEveryTestHooks ?? [];

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
                foreach (var entry in sourceHooks)
                {
                    var hook = entry.Materialize();
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
                    foreach (var entry in openTypeHooks)
                    {
                        var hook = entry.Materialize();
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

    public ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectBeforeClassHooksAsync(Type testClassType)
        => GetOrBuildHooksAsync(_beforeClassHooksCache, testClassType, _buildBeforeClassHooks);

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
                foreach (var entry in sourceHooks)
                {
                    var hook = entry.Materialize();
                    var namedHook = await CreateStaticHookDelegateAsync(hook);
                    typeHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
                }
            }

            // Also check the open generic type definition for generic types
            if (currentType is { IsGenericType: true, IsGenericTypeDefinition: false })
            {
                var openGenericType = GetCachedGenericTypeDefinition(currentType);
                if (Sources.BeforeClassHooks.TryGetValue(openGenericType, out var openTypeHooks))
                {
                    foreach (var entry in openTypeHooks)
                    {
                        var hook = entry.Materialize();
                        var namedHook = await CreateStaticHookDelegateAsync(hook);
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
            SortAndAddHooks(finalHooks, typeHooks);
        }

        return finalHooks;
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectAfterClassHooksAsync(Type testClassType)
        => GetOrBuildHooksAsync(_afterClassHooksCache, testClassType, _buildAfterClassHooks);

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
                foreach (var entry in sourceHooks)
                {
                    var hook = entry.Materialize();
                    var namedHook = await CreateStaticHookDelegateAsync(hook);
                    typeHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
                }
            }

            // Also check the open generic type definition for generic types
            if (currentType is { IsGenericType: true, IsGenericTypeDefinition: false })
            {
                var openGenericType = GetCachedGenericTypeDefinition(currentType);
                if (Sources.AfterClassHooks.TryGetValue(openGenericType, out var openTypeHooks))
                {
                    foreach (var entry in openTypeHooks)
                    {
                        var hook = entry.Materialize();
                        var namedHook = await CreateStaticHookDelegateAsync(hook);
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
            SortAndAddHooks(finalHooks, typeHooks);
        }

        return finalHooks;
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectBeforeAssemblyHooksAsync(Assembly assembly)
        => GetOrBuildHooksAsync(_beforeAssemblyHooksCache, assembly, _buildBeforeAssemblyHooks);

    private async Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> BuildBeforeAssemblyHooksAsync(Assembly assembly)
    {
        if (!Sources.BeforeAssemblyHooks.TryGetValue(assembly, out var assemblyHooks))
        {
            return [];
        }

        var allHooks = new List<(int order, int registrationIndex, NamedHookDelegate<AssemblyHookContext> hook)>(assemblyHooks.Count);

        foreach (var entry in assemblyHooks)
        {
            var hook = entry.Materialize();
            var namedHook = await CreateStaticHookDelegateAsync(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
        }

        return SortAndProject(allHooks);
    }

    public ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectAfterAssemblyHooksAsync(Assembly assembly)
        => GetOrBuildHooksAsync(_afterAssemblyHooksCache, assembly, _buildAfterAssemblyHooks);

    private async Task<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> BuildAfterAssemblyHooksAsync(Assembly assembly)
    {
        if (!Sources.AfterAssemblyHooks.TryGetValue(assembly, out var assemblyHooks))
        {
            return [];
        }

        var allHooks = new List<(int order, int registrationIndex, NamedHookDelegate<AssemblyHookContext> hook)>(assemblyHooks.Count);

        foreach (var entry in assemblyHooks)
        {
            var hook = entry.Materialize();
            var namedHook = await CreateStaticHookDelegateAsync(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, namedHook));
        }

        return SortAndProject(allHooks);
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

    private async ValueTask<NamedHookDelegate<TestContext>> CreateInstanceHookDelegateAsync(InstanceHookMethod hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);

        var name = hook.Name;

        return new NamedHookDelegate<TestContext>(name, async (context, cancellationToken) =>
        {
            // Precedence + skip/placeholder handling + CustomHookExecutor fallback all live
            // in InstanceHookMethod.ExecuteAsync (via ResolveEffectiveExecutor), matching the
            // static BeforeTestHookMethod/AfterTestHookMethod path.
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                (ctx, ct) => hook.ExecuteAsync(ctx, ct),
                context,
                hook.Timeout,
                hook.Name,
                cancellationToken);

            await timeoutAction();
        });
    }

    private async ValueTask<NamedHookDelegate<TContext>> CreateStaticHookDelegateAsync<TContext>(StaticHookMethod<TContext> hook)
    {
        await ProcessHookRegistrationAsync(hook);

        return new NamedHookDelegate<TContext>(hook.Name, (context, cancellationToken) =>
            HookTimeoutHelper.CreateTimeoutHookAction(hook, context, cancellationToken));
    }
}
