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
            var allHooks = new List<(int order, Func<TestContext, CancellationToken, Task> hook)>();

            var currentType = type;
            while (currentType != null)
            {
                if (Sources.BeforeTestHooks.TryGetValue(currentType, out var typeHooks))
                {
                    foreach (var hook in typeHooks)
                    {
                        var hookFunc = CreateInstanceHookDelegate(hook);
                        allHooks.Add((hook.Order, hookFunc));
                    }
                }
                currentType = currentType.BaseType;
            }

            return allHooks
                .OrderBy(h => h.order)
                .Select(h => h.hook)
                .ToList();
        });

        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectAfterTestHooksAsync(Type testClassType)
    {
        var hooks = _afterTestHooksCache.GetOrAdd(testClassType, type =>
        {
            var allHooks = new List<(int order, Func<TestContext, CancellationToken, Task> hook)>();

            var currentType = type;
            while (currentType != null)
            {
                if (Sources.AfterTestHooks.TryGetValue(currentType, out var typeHooks))
                {
                    foreach (var hook in typeHooks)
                    {
                        var hookFunc = CreateInstanceHookDelegate(hook);
                        allHooks.Add((hook.Order, hookFunc));
                    }
                }
                currentType = currentType.BaseType;
            }

            return allHooks
                .OrderBy(h => h.order)
                .Select(h => h.hook)
                .ToList();
        });

        return new ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectBeforeEveryTestHooksAsync(Type testClassType)
    {
        var hooks = _beforeEveryTestHooksCache.GetOrAdd(testClassType, type =>
        {
            var allHooks = new List<(int order, Func<TestContext, CancellationToken, Task> hook)>();

            var currentType = type;
            while (currentType != null)
            {
                if (Sources.BeforeEveryTestHooks.TryGetValue(currentType, out var typeHooks))
                {
                    foreach (var hook in typeHooks)
                    {
                        var hookFunc = CreateStaticHookDelegate(hook);
                        allHooks.Add((hook.Order, hookFunc));
                    }
                }
                currentType = currentType.BaseType;
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

            var currentType = type;
            while (currentType != null)
            {
                if (Sources.AfterEveryTestHooks.TryGetValue(currentType, out var typeHooks))
                {
                    foreach (var hook in typeHooks)
                    {
                        var hookFunc = CreateStaticHookDelegate(hook);
                        allHooks.Add((hook.Order, hookFunc));
                    }
                }
                currentType = currentType.BaseType;
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
            var allHooks = new List<(int order, Func<ClassHookContext, CancellationToken, Task> hook)>();

            var currentType = type;
            while (currentType != null)
            {
                if (Sources.BeforeClassHooks.TryGetValue(currentType, out var typeHooks))
                {
                    foreach (var hook in typeHooks)
                    {
                        var hookFunc = CreateClassHookDelegate(hook);
                        allHooks.Add((hook.Order, hookFunc));
                    }
                }
                currentType = currentType.BaseType;
            }

            return allHooks
                .OrderBy(h => h.order)
                .Select(h => h.hook)
                .ToList();
        });

        return new ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>>(hooks);
    }

    public ValueTask<IReadOnlyList<Func<ClassHookContext, CancellationToken, Task>>> CollectAfterClassHooksAsync(Type testClassType)
    {
        var hooks = _afterClassHooksCache.GetOrAdd(testClassType, type =>
        {
            var allHooks = new List<(int order, Func<ClassHookContext, CancellationToken, Task> hook)>();

            var currentType = type;
            while (currentType != null)
            {
                if (Sources.AfterClassHooks.TryGetValue(currentType, out var typeHooks))
                {
                    foreach (var hook in typeHooks)
                    {
                        var hookFunc = CreateClassHookDelegate(hook);
                        allHooks.Add((hook.Order, hookFunc));
                    }
                }
                currentType = currentType.BaseType;
            }

            return allHooks
                .OrderBy(h => h.order)
                .Select(h => h.hook)
                .ToList();
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

    private static Func<TestContext, CancellationToken, Task> CreateInstanceHookDelegate(InstanceHookMethod hook)
    {
        return async (context, cancellationToken) =>
        {
            if (hook.Body != null)
            {
                await hook.Body(
                    context.TestDetails.ClassInstance ?? throw new InvalidOperationException("ClassInstance is null"),
                    context,
                    cancellationToken);
            }
        };
    }

    private static Func<TestContext, CancellationToken, Task> CreateStaticHookDelegate(StaticHookMethod<TestContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            if (hook.Body != null)
            {
                await hook.Body(context, cancellationToken);
            }
        };
    }

    private static Func<ClassHookContext, CancellationToken, Task> CreateClassHookDelegate(StaticHookMethod<ClassHookContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            if (hook.Body != null)
            {
                await hook.Body(context, cancellationToken);
            }
        };
    }

    private static Func<AssemblyHookContext, CancellationToken, Task> CreateAssemblyHookDelegate(StaticHookMethod<AssemblyHookContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            if (hook.Body != null)
            {
                await hook.Body(context, cancellationToken);
            }
        };
    }

    private static Func<TestSessionContext, CancellationToken, Task> CreateTestSessionHookDelegate(StaticHookMethod<TestSessionContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            if (hook.Body != null)
            {
                await hook.Body(context, cancellationToken);
            }
        };
    }

    private static Func<BeforeTestDiscoveryContext, CancellationToken, Task> CreateBeforeTestDiscoveryHookDelegate(StaticHookMethod<BeforeTestDiscoveryContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            if (hook.Body != null)
            {
                await hook.Body(context, cancellationToken);
            }
        };
    }

    private static Func<TestDiscoveryContext, CancellationToken, Task> CreateTestDiscoveryHookDelegate(StaticHookMethod<TestDiscoveryContext> hook)
    {
        return async (context, cancellationToken) =>
        {
            if (hook.Body != null)
            {
                await hook.Body(context, cancellationToken);
            }
        };
    }

}
