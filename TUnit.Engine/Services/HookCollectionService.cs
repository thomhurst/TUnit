using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Engine.Helpers;
using TUnit.Engine.Interfaces;

namespace TUnit.Engine.Services;

internal sealed class HookCollectionService : IHookCollectionService
{
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
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

    public HookCollectionService(EventReceiverOrchestrator eventReceiverOrchestrator)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
    }

    private static Type GetCachedGenericTypeDefinition(Type type)
    {
        return _genericTypeDefinitionCache.GetOrAdd(type, t => t.GetGenericTypeDefinition());
    }

    public async ValueTask InitializeAsync()
    {
        // Pre-compute all global hooks that don't depend on specific types/assemblies
        _beforeEveryTestHooks = await BuildGlobalBeforeEveryTestHooksAsync();
        _afterEveryTestHooks = await BuildGlobalAfterEveryTestHooksAsync();
        _beforeTestSessionHooks = BuildGlobalBeforeTestSessionHooks();
        _afterTestSessionHooks = BuildGlobalAfterTestSessionHooks();
        _beforeTestDiscoveryHooks = BuildGlobalBeforeTestDiscoveryHooks();
        _afterTestDiscoveryHooks = BuildGlobalAfterTestDiscoveryHooks();
        _beforeEveryClassHooks = BuildGlobalBeforeEveryClassHooks();
        _afterEveryClassHooks = BuildGlobalAfterEveryClassHooks();
        _beforeEveryAssemblyHooks = BuildGlobalBeforeEveryAssemblyHooks();
        _afterEveryAssemblyHooks = BuildGlobalAfterEveryAssemblyHooks();
    }

    private async Task<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> BuildGlobalBeforeEveryTestHooksAsync()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<TestContext, CancellationToken, Task> hook)>(Sources.BeforeEveryTestHooks.Count);

        foreach (var hook in Sources.BeforeEveryTestHooks)
        {
            var hookFunc = await CreateStaticHookDelegateAsync(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        return allHooks
            .OrderBy(static h => h.order)
            .ThenBy(static h => h.registrationIndex)
            .Select(static h => h.hook)
            .ToList();
    }

    private async Task<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> BuildGlobalAfterEveryTestHooksAsync()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<TestContext, CancellationToken, Task> hook)>(Sources.AfterEveryTestHooks.Count);

        foreach (var hook in Sources.AfterEveryTestHooks)
        {
            var hookFunc = await CreateStaticHookDelegateAsync(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        return allHooks
            .OrderBy(static h => h.order)
            .ThenBy(static h => h.registrationIndex)
            .Select(static h => h.hook)
            .ToList();
    }

    private IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>> BuildGlobalBeforeTestSessionHooks()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<TestSessionContext, CancellationToken, Task> hook)>(Sources.BeforeTestSessionHooks.Count);

        foreach (var hook in Sources.BeforeTestSessionHooks)
        {
            var hookFunc = CreateTestSessionHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        return allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();
    }

    private IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>> BuildGlobalAfterTestSessionHooks()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<TestSessionContext, CancellationToken, Task> hook)>(Sources.AfterTestSessionHooks.Count);

        foreach (var hook in Sources.AfterTestSessionHooks)
        {
            var hookFunc = CreateTestSessionHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        return allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();
    }

    private IReadOnlyList<Func<BeforeTestDiscoveryContext, CancellationToken, Task>> BuildGlobalBeforeTestDiscoveryHooks()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<BeforeTestDiscoveryContext, CancellationToken, Task> hook)>(Sources.BeforeTestDiscoveryHooks.Count);

        foreach (var hook in Sources.BeforeTestDiscoveryHooks)
        {
            var hookFunc = CreateBeforeTestDiscoveryHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        return allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();
    }

    private IReadOnlyList<Func<TestDiscoveryContext, CancellationToken, Task>> BuildGlobalAfterTestDiscoveryHooks()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<TestDiscoveryContext, CancellationToken, Task> hook)>(Sources.AfterTestDiscoveryHooks.Count);

        foreach (var hook in Sources.AfterTestDiscoveryHooks)
        {
            var hookFunc = CreateTestDiscoveryHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        return allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();
    }

    private IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>> BuildGlobalBeforeEveryClassHooks()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<ClassHookContext, CancellationToken, Task> hook)>(Sources.BeforeEveryClassHooks.Count);

        foreach (var hook in Sources.BeforeEveryClassHooks)
        {
            var hookFunc = CreateClassHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        return allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();
    }

    private IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>> BuildGlobalAfterEveryClassHooks()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<ClassHookContext, CancellationToken, Task> hook)>(Sources.AfterEveryClassHooks.Count);

        foreach (var hook in Sources.AfterEveryClassHooks)
        {
            var hookFunc = CreateClassHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        return allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();
    }

    private IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>> BuildGlobalBeforeEveryAssemblyHooks()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<AssemblyHookContext, CancellationToken, Task> hook)>(Sources.BeforeEveryAssemblyHooks.Count);

        foreach (var hook in Sources.BeforeEveryAssemblyHooks)
        {
            var hookFunc = CreateAssemblyHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        return allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();
    }

    private IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>> BuildGlobalAfterEveryAssemblyHooks()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<AssemblyHookContext, CancellationToken, Task> hook)>(Sources.AfterEveryAssemblyHooks.Count);

        foreach (var hook in Sources.AfterEveryAssemblyHooks)
        {
            var hookFunc = CreateAssemblyHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        return allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();
    }

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

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectBeforeClassHooksAsync(Type testClassType)
    {
        var hooks = _beforeClassHooksCache.GetOrAdd(testClassType, type =>
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
                        var hookFunc = CreateClassHookDelegate(hook);
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
                            var hookFunc = CreateClassHookDelegate(hook);
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
        });

        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectAfterClassHooksAsync(Type testClassType)
    {
        var hooks = _afterClassHooksCache.GetOrAdd(testClassType, type =>
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
                        var hookFunc = CreateClassHookDelegate(hook);
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
                            var hookFunc = CreateClassHookDelegate(hook);
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
        });

        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectBeforeAssemblyHooksAsync(Assembly assembly)
    {
        var hooks = _beforeAssemblyHooksCache.GetOrAdd(assembly, asm =>
        {
            if (!Sources.BeforeAssemblyHooks.TryGetValue(asm, out var assemblyHooks))
            {
                return [];
            }

            var allHooks = new List<(int order, Func<AssemblyHookContext, CancellationToken, Task> hook)>(assemblyHooks.Count);

            foreach (var hook in assemblyHooks)
            {
                var hookFunc = CreateAssemblyHookDelegate(hook);
                allHooks.Add((hook.Order, hookFunc));
            }

            return allHooks
                .OrderBy(h => h.order)
                .Select(h => h.hook)
                .ToList();
        });

        return new ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectAfterAssemblyHooksAsync(Assembly assembly)
    {
        var hooks = _afterAssemblyHooksCache.GetOrAdd(assembly, asm =>
        {
            if (!Sources.AfterAssemblyHooks.TryGetValue(asm, out var assemblyHooks))
            {
                return [];
            }

            var allHooks = new List<(int order, Func<AssemblyHookContext, CancellationToken, Task> hook)>(assemblyHooks.Count);

            foreach (var hook in assemblyHooks)
            {
                var hookFunc = CreateAssemblyHookDelegate(hook);
                allHooks.Add((hook.Order, hookFunc));
            }

            return allHooks
                .OrderBy(h => h.order)
                .Select(h => h.hook)
                .ToList();
        });

        return new ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>>(hooks);
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

        return async (context, cancellationToken) =>
        {
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);

            await timeoutAction();
        };
    }

    private static Func<ClassHookContext, CancellationToken, Task> CreateClassHookDelegate(StaticHookMethod<ClassHookContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);

            await timeoutAction();
        };
    }

    private static Func<AssemblyHookContext, CancellationToken, Task> CreateAssemblyHookDelegate(StaticHookMethod<AssemblyHookContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);

            await timeoutAction();
        };
    }

    private static Func<TestSessionContext, CancellationToken, Task> CreateTestSessionHookDelegate(StaticHookMethod<TestSessionContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);

            await timeoutAction();
        };
    }

    private static Func<BeforeTestDiscoveryContext, CancellationToken, Task> CreateBeforeTestDiscoveryHookDelegate(StaticHookMethod<BeforeTestDiscoveryContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);

            await timeoutAction();
        };
    }

    private static Func<TestDiscoveryContext, CancellationToken, Task> CreateTestDiscoveryHookDelegate(StaticHookMethod<TestDiscoveryContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);

            await timeoutAction();
        };
    }

}
