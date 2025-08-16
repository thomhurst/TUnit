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

    private void ProcessHookRegistration(HookMethod hookMethod, CancellationToken cancellationToken = default)
    {
        // Only process each hook once
        if (!_processedHooks.TryAdd(hookMethod, true))
        {
            return;
        }

        try
        {
            var context = new HookRegisteredContext(hookMethod);

            _eventReceiverOrchestrator.InvokeHookRegistrationEventReceiversAsync(context, cancellationToken).GetAwaiter().GetResult();
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
        if (_beforeTestHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(cachedHooks);
        }

        var hooks = BuildBeforeTestHooks(testClassType);
        _beforeTestHooksCache.TryAdd(testClassType, hooks);
        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    private IReadOnlyList<Func<TestContext, CancellationToken, Task>> BuildBeforeTestHooks(Type type)
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
                        var hookFunc = CreateInstanceHookDelegate(hook);
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
                            var hookFunc = CreateInstanceHookDelegate(hook);
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
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterTestHooksAsync(Type testClassType)
    {
        if (_afterTestHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(cachedHooks);
        }

        var hooks = BuildAfterTestHooks(testClassType);
        _afterTestHooksCache.TryAdd(testClassType, hooks);
        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    private IReadOnlyList<Func<TestContext, CancellationToken, Task>> BuildAfterTestHooks(Type type)
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
                        var hookFunc = CreateInstanceHookDelegate(hook);
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
                            var hookFunc = CreateInstanceHookDelegate(hook);
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
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectBeforeEveryTestHooksAsync(Type testClassType)
    {
        if (_beforeEveryTestHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(cachedHooks);
        }

        var hooks = BuildBeforeEveryTestHooks(testClassType);
        _beforeEveryTestHooksCache.TryAdd(testClassType, hooks);
        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    private IReadOnlyList<Func<TestContext, CancellationToken, Task>> BuildBeforeEveryTestHooks(Type type)
        {
            var allHooks = new List<(int order, int registrationIndex, Func<TestContext, CancellationToken, Task> hook)>();

            // Collect all global BeforeEvery hooks
            foreach (var hook in Sources.BeforeEveryTestHooks)
            {
                var hookFunc = CreateStaticHookDelegate(hook);
                allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
            }

            var hooks = allHooks
                .OrderBy(h => h.order)
                .ThenBy(h => h.registrationIndex)
                .Select(h => h.hook)
                .ToList();
            return hooks;
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterEveryTestHooksAsync(Type testClassType)
    {
        if (_afterEveryTestHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(cachedHooks);
        }

        var hooks = BuildAfterEveryTestHooks(testClassType);
        _afterEveryTestHooksCache.TryAdd(testClassType, hooks);
        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    private IReadOnlyList<Func<TestContext, CancellationToken, Task>> BuildAfterEveryTestHooks(Type type)
        {
            var allHooks = new List<(int order, int registrationIndex, Func<TestContext, CancellationToken, Task> hook)>();

            // Collect all global AfterEvery hooks
            foreach (var hook in Sources.AfterEveryTestHooks)
            {
                var hookFunc = CreateStaticHookDelegate(hook);
                allHooks.Add((hook.Order, hook.RegistrationIndex, hookFunc));
            }

            var hooks = allHooks
                .OrderBy(h => h.order)
                .ThenBy(h => h.registrationIndex)
                .Select(h => h.hook)
                .ToList();
            return hooks;
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectBeforeClassHooksAsync(Type testClassType)
    {
        if (_beforeClassHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(cachedHooks);
        }

        var hooks = BuildBeforeClassHooks(testClassType);
        _beforeClassHooksCache.TryAdd(testClassType, hooks);
        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    private IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>> BuildBeforeClassHooks(Type type)
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
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectAfterClassHooksAsync(Type testClassType)
    {
        if (_afterClassHooksCache.TryGetValue(testClassType, out var cachedHooks))
        {
            return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(cachedHooks);
        }

        var hooks = BuildAfterClassHooks(testClassType);
        _afterClassHooksCache.TryAdd(testClassType, hooks);
        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    private IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>> BuildAfterClassHooks(Type type)
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

    private Func<TestContext, CancellationToken, Task> CreateInstanceHookDelegate(InstanceHookMethod hook)
    {
        // Process hook registration event receivers to handle skip attributes
        ProcessHookRegistration(hook);
        
        return async (context, cancellationToken) =>
        {
            // Check if hook should be skipped
            if (!string.IsNullOrEmpty(hook.SkipReason))
            {
                return; // Skip this hook execution
            }
            
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                (ctx, ct) => hook.ExecuteAsync(ctx, ct),
                context,
                hook.Timeout,
                hook.Name,
                cancellationToken);
            
            await timeoutAction();
        };
    }

    private Func<TestContext, CancellationToken, Task> CreateStaticHookDelegate(StaticHookMethod<TestContext> hook)
    {
        // Process hook registration event receivers to handle skip attributes
        ProcessHookRegistration(hook);
        
        return async (context, cancellationToken) =>
        {
            // Check if hook should be skipped
            if (!string.IsNullOrEmpty(hook.SkipReason))
            {
                return; // Skip this hook execution
            }
            
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
            
            await timeoutAction();
        };
    }

    private Func<ClassHookContext, CancellationToken, Task> CreateClassHookDelegate(StaticHookMethod<ClassHookContext> hook)
    {
        // Process hook registration event receivers to handle skip attributes
        ProcessHookRegistration(hook);
        
        return async (context, cancellationToken) =>
        {
            // Check if hook should be skipped
            if (!string.IsNullOrEmpty(hook.SkipReason))
            {
                return; // Skip this hook execution
            }
            
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
            
            await timeoutAction();
        };
    }

    private Func<AssemblyHookContext, CancellationToken, Task> CreateAssemblyHookDelegate(StaticHookMethod<AssemblyHookContext> hook)
    {
        // Process hook registration event receivers to handle skip attributes
        ProcessHookRegistration(hook);
        
        return async (context, cancellationToken) =>
        {
            // Check if hook should be skipped
            if (!string.IsNullOrEmpty(hook.SkipReason))
            {
                return; // Skip this hook execution
            }
            
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
            
            await timeoutAction();
        };
    }

    private Func<TestSessionContext, CancellationToken, Task> CreateTestSessionHookDelegate(StaticHookMethod<TestSessionContext> hook)
    {
        // Process hook registration event receivers to handle skip attributes
        ProcessHookRegistration(hook);
        
        return async (context, cancellationToken) =>
        {
            // Check if hook should be skipped
            if (!string.IsNullOrEmpty(hook.SkipReason))
            {
                return; // Skip this hook execution
            }
            
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
            
            await timeoutAction();
        };
    }

    private Func<BeforeTestDiscoveryContext, CancellationToken, Task> CreateBeforeTestDiscoveryHookDelegate(StaticHookMethod<BeforeTestDiscoveryContext> hook)
    {
        // Process hook registration event receivers to handle skip attributes
        ProcessHookRegistration(hook);
        
        return async (context, cancellationToken) =>
        {
            // Check if hook should be skipped
            if (!string.IsNullOrEmpty(hook.SkipReason))
            {
                return; // Skip this hook execution
            }
            
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
            
            await timeoutAction();
        };
    }

    private Func<TestDiscoveryContext, CancellationToken, Task> CreateTestDiscoveryHookDelegate(StaticHookMethod<TestDiscoveryContext> hook)
    {
        // Process hook registration event receivers to handle skip attributes
        ProcessHookRegistration(hook);
        
        return async (context, cancellationToken) =>
        {
            // Check if hook should be skipped
            if (!string.IsNullOrEmpty(hook.SkipReason))
            {
                return; // Skip this hook execution
            }
            
            var timeoutAction = HookTimeoutHelper.CreateTimeoutHookAction(
                hook,
                context,
                cancellationToken);
            
            await timeoutAction();
        };
    }

}
