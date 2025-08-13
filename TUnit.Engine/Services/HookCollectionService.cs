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
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<TestContext, CancellationToken, Task>>> _beforeEveryTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<TestContext, CancellationToken, Task>>> _afterEveryTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> _beforeClassHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> _afterClassHooksCache = new();
    
    // Cache for complete hook chains to avoid repeated lookups
    private readonly ConcurrentDictionary<Type, CompleteHookChain> _completeHookChainCache = new();
    
    // Cache for processed hooks to avoid re-processing event receivers
    private readonly ConcurrentDictionary<object, bool> _processedHooks = new();

    public HookCollectionService(EventReceiverOrchestrator eventReceiverOrchestrator)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
    }

    private async Task ProcessHookRegistrationAsync(object hookMethod, CancellationToken cancellationToken = default)
    {
        // Only process each hook once
        if (!_processedHooks.TryAdd(hookMethod, true))
        {
            return;
        }

        try
        {
            HookRegisteredContext context;
            
            if (hookMethod is StaticHookMethod staticHook)
            {
                context = new HookRegisteredContext(staticHook);
            }
            else if (hookMethod is InstanceHookMethod instanceHook)
            {
                context = new HookRegisteredContext(instanceHook);
            }
            else
            {
                return; // Unknown hook type
            }

            await _eventReceiverOrchestrator.InvokeHookRegistrationEventReceiversAsync(context, cancellationToken);
        }
        catch (Exception)
        {
            // Ignore errors during hook registration event processing to avoid breaking hook execution
            // The EventReceiverOrchestrator already logs errors internally
        }
    }
    
    private sealed class CompleteHookChain
    {
        public IReadOnlyList<Func<TestContext, CancellationToken, Task>> BeforeTestHooks { get; init; } = [
        ];
        public IReadOnlyList<Func<TestContext, CancellationToken, Task>> AfterTestHooks { get; init; } = [
        ];
        public IReadOnlyList<Func<TestContext, CancellationToken, Task>> BeforeEveryTestHooks { get; init; } = [
        ];
        public IReadOnlyList<Func<TestContext, CancellationToken, Task>> AfterEveryTestHooks { get; init; } = [
        ];
        public IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>> BeforeClassHooks { get; init; } = [
        ];
        public IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>> AfterClassHooks { get; init; } = [
        ];
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectBeforeTestHooksAsync(Type testClassType)
    {
        var hooks = _beforeTestHooksCache.GetOrAdd(testClassType, type =>
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
                    var openGenericType = currentType.GetGenericTypeDefinition();
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
                finalHooks.AddRange(typeHooks.OrderBy(h => h.order).ThenBy(h => h.registrationIndex).Select(h => h.hook));
            }

            return finalHooks;
        });

        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterTestHooksAsync(Type testClassType)
    {
        var hooks = _afterTestHooksCache.GetOrAdd(testClassType, type =>
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
                    var openGenericType = currentType.GetGenericTypeDefinition();
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
                finalHooks.AddRange(typeHooks.OrderBy(h => h.order).ThenBy(h => h.registrationIndex).Select(h => h.hook));
            }

            return finalHooks;
        });

        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectBeforeEveryTestHooksAsync(Type testClassType)
    {
        var hooks = _beforeEveryTestHooksCache.GetOrAdd(testClassType, type =>
        {
            var allHooks = new List<(int order, int registrationIndex, Func<TestContext, CancellationToken, Task> hook)>();

            // Collect all global BeforeEvery hooks
            foreach (var hook in Sources.BeforeEveryTestHooks)
            {
                var hookFunc = await CreateStaticHookDelegateAsync(hook);
                allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
            }

            return allHooks
                .OrderBy(h => h.order)
                .ThenBy(h => h.registrationIndex)
                .Select(h => h.hook)
                .ToList();
        });

        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterEveryTestHooksAsync(Type testClassType)
    {
        var hooks = _afterEveryTestHooksCache.GetOrAdd(testClassType, type =>
        {
            var allHooks = new List<(int order, int registrationIndex, Func<TestContext, CancellationToken, Task> hook)>();

            // Collect all global AfterEvery hooks
            foreach (var hook in Sources.AfterEveryTestHooks)
            {
                var hookFunc = await CreateStaticHookDelegateAsync(hook);
                allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
            }

            return allHooks
                .OrderBy(h => h.order)
                .ThenBy(h => h.registrationIndex)
                .Select(h => h.hook)
                .ToList();
        });

        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
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
                    var openGenericType = currentType.GetGenericTypeDefinition();
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

            // For Before hooks: base class hooks run first
            // Reverse the list since we collected from derived to base
            hooksByType.Reverse();

            var finalHooks = new List<Func<ClassHookContext, CancellationToken, Task>>();
            foreach (var (_, typeHooks) in hooksByType)
            {
                // Within each type level, sort by Order then by RegistrationIndex
                finalHooks.AddRange(typeHooks.OrderBy(h => h.order).ThenBy(h => h.registrationIndex).Select(h => h.hook));
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
                    var openGenericType = currentType.GetGenericTypeDefinition();
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

            // For After hooks: derived class hooks run first
            // No need to reverse since we collected from derived to base

            var finalHooks = new List<Func<ClassHookContext, CancellationToken, Task>>();
            foreach (var (_, typeHooks) in hooksByType)
            {
                // Within each type level, sort by Order then by RegistrationIndex
                finalHooks.AddRange(typeHooks.OrderBy(h => h.order).ThenBy(h => h.registrationIndex).Select(h => h.hook));
            }

            return finalHooks;
        });

        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectBeforeAssemblyHooksAsync(Assembly assembly)
    {
        var allHooks = new List<(int order, Func<AssemblyHookContext, CancellationToken, Task> hook)>();

        if (Sources.BeforeAssemblyHooks.TryGetValue(assembly, out var assemblyHooks))
        {
            foreach (var hook in assemblyHooks)
            {
                var hookFunc = CreateAssemblyHookDelegate(hook);
                allHooks.Add((hook.Order, hookFunc));
            }
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectAfterAssemblyHooksAsync(Assembly assembly)
    {
        var allHooks = new List<(int order, Func<AssemblyHookContext, CancellationToken, Task> hook)>();

        if (Sources.AfterAssemblyHooks.TryGetValue(assembly, out var assemblyHooks))
        {
            foreach (var hook in assemblyHooks)
            {
                var hookFunc = CreateAssemblyHookDelegate(hook);
                allHooks.Add((hook.Order, hookFunc));
            }
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>> CollectBeforeTestSessionHooksAsync()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<TestSessionContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.BeforeTestSessionHooks)
        {
            var hookFunc = CreateTestSessionHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>> CollectAfterTestSessionHooksAsync()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<TestSessionContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.AfterTestSessionHooks)
        {
            var hookFunc = CreateTestSessionHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<BeforeTestDiscoveryContext, CancellationToken, Task>>> CollectBeforeTestDiscoveryHooksAsync()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<BeforeTestDiscoveryContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.BeforeTestDiscoveryHooks)
        {
            var hookFunc = CreateBeforeTestDiscoveryHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<BeforeTestDiscoveryContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestDiscoveryContext, CancellationToken, Task>>> CollectAfterTestDiscoveryHooksAsync()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<TestDiscoveryContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.AfterTestDiscoveryHooks)
        {
            var hookFunc = CreateTestDiscoveryHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<TestDiscoveryContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectBeforeEveryClassHooksAsync()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<ClassHookContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.BeforeEveryClassHooks)
        {
            var hookFunc = CreateClassHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectAfterEveryClassHooksAsync()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<ClassHookContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.AfterEveryClassHooks)
        {
            var hookFunc = CreateClassHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectBeforeEveryAssemblyHooksAsync()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<AssemblyHookContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.BeforeEveryAssemblyHooks)
        {
            var hookFunc = CreateAssemblyHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectAfterEveryAssemblyHooksAsync()
    {
        var allHooks = new List<(int order, int registrationIndex, Func<AssemblyHookContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.AfterEveryAssemblyHooks)
        {
            var hookFunc = CreateAssemblyHookDelegate(hook);
            allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .ThenBy(h => h.registrationIndex)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>>(hooks);
    }

    private async Task<Func<TestContext, CancellationToken, Task>> CreateInstanceHookDelegateAsync(InstanceHookMethod hook)
    {
        // Process hook registration event receivers
        await ProcessHookRegistrationAsync(hook);
        
        return async (context, cancellationToken) =>
        {
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                (ctx, ct) => hook.ExecuteAsync(ctx, ct),
                context,
                hook.Timeout,
                hook.Name,
                cancellationToken);
            
            await timeoutAction();
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
