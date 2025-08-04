using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Engine.Interfaces;

namespace TUnit.Engine.Services;

internal sealed class HookCollectionService : IHookCollectionService
{
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<TestContext, CancellationToken, Task>>> _beforeTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<TestContext, CancellationToken, Task>>> _afterTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<TestContext, CancellationToken, Task>>> _beforeEveryTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<TestContext, CancellationToken, Task>>> _afterEveryTestHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> _beforeClassHooksCache = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> _afterClassHooksCache = new();

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectBeforeTestHooksAsync(Type testClassType)
    {
        var hooks = _beforeTestHooksCache.GetOrAdd(testClassType, type =>
        {
            var hooksByType = new List<(Type type, List<(int order, Func<TestContext, CancellationToken, Task> hook)> hooks)>();

            // Collect hooks for each type in the hierarchy
            var currentType = type;
            while (currentType != null)
            {
                var typeHooks = new List<(int order, Func<TestContext, CancellationToken, Task> hook)>();

                if (Sources.BeforeTestHooks.TryGetValue(currentType, out var sourceHooks))
                {
                    foreach (var hook in sourceHooks)
                    {
                        var hookFunc = CreateInstanceHookDelegate(hook);
                        typeHooks.Add((hook.Order, hookFunc));
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
                            typeHooks.Add((hook.Order, hookFunc));
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
                // Within each type level, sort by Order
                finalHooks.AddRange(typeHooks.OrderBy(h => h.order).Select(h => h.hook));
            }

            return finalHooks;
        });

        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterTestHooksAsync(Type testClassType)
    {
        var hooks = _afterTestHooksCache.GetOrAdd(testClassType, type =>
        {
            var hooksByType = new List<(Type type, List<(int order, Func<TestContext, CancellationToken, Task> hook)> hooks)>();

            // Collect hooks for each type in the hierarchy
            var currentType = type;
            while (currentType != null)
            {
                var typeHooks = new List<(int order, Func<TestContext, CancellationToken, Task> hook)>();

                if (Sources.AfterTestHooks.TryGetValue(currentType, out var sourceHooks))
                {
                    foreach (var hook in sourceHooks)
                    {
                        var hookFunc = CreateInstanceHookDelegate(hook);
                        typeHooks.Add((hook.Order, hookFunc));
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
                            typeHooks.Add((hook.Order, hookFunc));
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
                // Within each type level, sort by Order
                finalHooks.AddRange(typeHooks.OrderBy(h => h.order).Select(h => h.hook));
            }

            return finalHooks;
        });

        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectBeforeEveryTestHooksAsync(Type testClassType)
    {
        var hooks = _beforeEveryTestHooksCache.GetOrAdd(testClassType, type =>
        {
            var allHooks = new List<(int order, Func<TestContext, CancellationToken, Task> hook)>();

            // Collect all global BeforeEvery hooks
            foreach (var hook in Sources.BeforeEveryTestHooks)
            {
                var hookFunc = CreateStaticHookDelegate(hook);
                allHooks.Add((hook.Order, hookFunc));
            }

            return allHooks
                .OrderBy(h => h.order)
                .Select(h => h.hook)
                .ToList();
        });

        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterEveryTestHooksAsync(Type testClassType)
    {
        var hooks = _afterEveryTestHooksCache.GetOrAdd(testClassType, type =>
        {
            var allHooks = new List<(int order, Func<TestContext, CancellationToken, Task> hook)>();

            // Collect all global AfterEvery hooks
            foreach (var hook in Sources.AfterEveryTestHooks)
            {
                var hookFunc = CreateStaticHookDelegate(hook);
                allHooks.Add((hook.Order, hookFunc));
            }

            return allHooks
                .OrderBy(h => h.order)
                .Select(h => h.hook)
                .ToList();
        });

        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectBeforeClassHooksAsync(Type testClassType)
    {
        var hooks = _beforeClassHooksCache.GetOrAdd(testClassType, type =>
        {
            var hooksByType = new List<(Type type, List<(int order, Func<ClassHookContext, CancellationToken, Task> hook)> hooks)>();

            // Collect hooks for each type in the hierarchy
            var currentType = type;
            while (currentType != null)
            {
                var typeHooks = new List<(int order, Func<ClassHookContext, CancellationToken, Task> hook)>();

                if (Sources.BeforeClassHooks.TryGetValue(currentType, out var sourceHooks))
                {
                    foreach (var hook in sourceHooks)
                    {
                        var hookFunc = CreateClassHookDelegate(hook);
                        typeHooks.Add((hook.Order, hookFunc));
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
                            typeHooks.Add((hook.Order, hookFunc));
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
                // Within each type level, sort by Order
                finalHooks.AddRange(typeHooks.OrderBy(h => h.order).Select(h => h.hook));
            }

            return finalHooks;
        });

        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectAfterClassHooksAsync(Type testClassType)
    {
        var hooks = _afterClassHooksCache.GetOrAdd(testClassType, type =>
        {
            var hooksByType = new List<(Type type, List<(int order, Func<ClassHookContext, CancellationToken, Task> hook)> hooks)>();

            // Collect hooks for each type in the hierarchy
            var currentType = type;
            while (currentType != null)
            {
                var typeHooks = new List<(int order, Func<ClassHookContext, CancellationToken, Task> hook)>();

                if (Sources.AfterClassHooks.TryGetValue(currentType, out var sourceHooks))
                {
                    foreach (var hook in sourceHooks)
                    {
                        var hookFunc = CreateClassHookDelegate(hook);
                        typeHooks.Add((hook.Order, hookFunc));
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
                            typeHooks.Add((hook.Order, hookFunc));
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
                // Within each type level, sort by Order
                finalHooks.AddRange(typeHooks.OrderBy(h => h.order).Select(h => h.hook));
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
        var allHooks = new List<(int order, Func<TestSessionContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.BeforeTestSessionHooks)
        {
            var hookFunc = CreateTestSessionHookDelegate(hook);
            allHooks.Add((hook.Order, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>> CollectAfterTestSessionHooksAsync()
    {
        var allHooks = new List<(int order, Func<TestSessionContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.AfterTestSessionHooks)
        {
            var hookFunc = CreateTestSessionHookDelegate(hook);
            allHooks.Add((hook.Order, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<TestSessionContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<BeforeTestDiscoveryContext, CancellationToken, Task>>> CollectBeforeTestDiscoveryHooksAsync()
    {
        var allHooks = new List<(int order, Func<BeforeTestDiscoveryContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.BeforeTestDiscoveryHooks)
        {
            var hookFunc = CreateBeforeTestDiscoveryHookDelegate(hook);
            allHooks.Add((hook.Order, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<BeforeTestDiscoveryContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestDiscoveryContext, CancellationToken, Task>>> CollectAfterTestDiscoveryHooksAsync()
    {
        var allHooks = new List<(int order, Func<TestDiscoveryContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.AfterTestDiscoveryHooks)
        {
            var hookFunc = CreateTestDiscoveryHookDelegate(hook);
            allHooks.Add((hook.Order, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<TestDiscoveryContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectBeforeEveryClassHooksAsync()
    {
        var allHooks = new List<(int order, Func<ClassHookContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.BeforeEveryClassHooks)
        {
            var hookFunc = CreateClassHookDelegate(hook);
            allHooks.Add((hook.Order, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectAfterEveryClassHooksAsync()
    {
        var allHooks = new List<(int order, Func<ClassHookContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.AfterEveryClassHooks)
        {
            var hookFunc = CreateClassHookDelegate(hook);
            allHooks.Add((hook.Order, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectBeforeEveryAssemblyHooksAsync()
    {
        var allHooks = new List<(int order, Func<AssemblyHookContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.BeforeEveryAssemblyHooks)
        {
            var hookFunc = CreateAssemblyHookDelegate(hook);
            allHooks.Add((hook.Order, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>> CollectAfterEveryAssemblyHooksAsync()
    {
        var allHooks = new List<(int order, Func<AssemblyHookContext, CancellationToken, Task> hook)>();

        foreach (var hook in Sources.AfterEveryAssemblyHooks)
        {
            var hookFunc = CreateAssemblyHookDelegate(hook);
            allHooks.Add((hook.Order, hookFunc));
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<AssemblyHookContext, CancellationToken, Task>>>(hooks);
    }

    private static Func<TestContext, CancellationToken, Task> CreateInstanceHookDelegate(InstanceHookMethod hook)
    {
        return async (context, cancellationToken) =>
        {
            await hook.ExecuteAsync(context, cancellationToken);
        };
    }

    private static Func<TestContext, CancellationToken, Task> CreateStaticHookDelegate(StaticHookMethod<TestContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            await hook.ExecuteAsync(context, cancellationToken);
        };
    }

    private static Func<ClassHookContext, CancellationToken, Task> CreateClassHookDelegate(StaticHookMethod<ClassHookContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            await hook.ExecuteAsync(context, cancellationToken);
        };
    }

    private static Func<AssemblyHookContext, CancellationToken, Task> CreateAssemblyHookDelegate(StaticHookMethod<AssemblyHookContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            await hook.ExecuteAsync(context, cancellationToken);
        };
    }

    private static Func<TestSessionContext, CancellationToken, Task> CreateTestSessionHookDelegate(StaticHookMethod<TestSessionContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            await hook.ExecuteAsync(context, cancellationToken);
        };
    }

    private static Func<BeforeTestDiscoveryContext, CancellationToken, Task> CreateBeforeTestDiscoveryHookDelegate(StaticHookMethod<BeforeTestDiscoveryContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            await hook.ExecuteAsync(context, cancellationToken);
        };
    }

    private static Func<TestDiscoveryContext, CancellationToken, Task> CreateTestDiscoveryHookDelegate(StaticHookMethod<TestDiscoveryContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            await hook.ExecuteAsync(context, cancellationToken);
        };
    }

}
