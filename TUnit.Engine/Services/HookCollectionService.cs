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

    // Type-indexed hook registry for O(1) lookup
    private readonly Lazy<Dictionary<Type, List<(int order, InstanceHookMethod hook)>>> _beforeTestHooksByType;
    private readonly Lazy<Dictionary<Type, List<(int order, InstanceHookMethod hook)>>> _afterTestHooksByType;

    public HookCollectionService()
    {
        _beforeTestHooksByType = new Lazy<Dictionary<Type, List<(int, InstanceHookMethod)>>>(BuildBeforeTestHooksIndex);
        _afterTestHooksByType = new Lazy<Dictionary<Type, List<(int, InstanceHookMethod)>>>(BuildAfterTestHooksIndex);
    }

    public ValueTask<IReadOnlyList<Func<TestContext, CancellationToken, Task>>> CollectBeforeTestHooksAsync(Type testClassType)
    {
        var hooks = _beforeTestHooksCache.GetOrAdd(testClassType, type =>
        {
            var allHooks = new List<(int order, Func<TestContext, CancellationToken, Task> hook)>();
            var hooksByType = _beforeTestHooksByType.Value;

            // Walk up the inheritance chain - O(inheritance_depth) instead of O(all_hooks)
            var currentType = type;
            while (currentType != null)
            {
                if (hooksByType.TryGetValue(currentType, out var typeHooks))
                {
                    foreach (var (order, hook) in typeHooks)
                    {
                        var hookFunc = CreateInstanceHookDelegate(hook);
                        allHooks.Add((order, hookFunc));
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
            var hooksByType = _afterTestHooksByType.Value;

            // Walk up the inheritance chain - O(inheritance_depth) instead of O(all_hooks)
            var currentType = type;
            while (currentType != null)
            {
                if (hooksByType.TryGetValue(currentType, out var typeHooks))
                {
                    foreach (var (order, hook) in typeHooks)
                    {
                        var hookFunc = CreateInstanceHookDelegate(hook);
                        allHooks.Add((order, hookFunc));
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

            foreach (var source in Sources.TestHookSources)
            {
                var sourceHooks = source.CollectBeforeEveryTestHooks(string.Empty);
                foreach (var hook in sourceHooks)
                {
                    var hookFunc = CreateStaticHookDelegate(hook);
                    allHooks.Add((hook.Order, hookFunc));
                }
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

            foreach (var source in Sources.TestHookSources)
            {
                var sourceHooks = source.CollectAfterEveryTestHooks(string.Empty);
                foreach (var hook in sourceHooks)
                {
                    var hookFunc = CreateStaticHookDelegate(hook);
                    allHooks.Add((hook.Order, hookFunc));
                }
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

            foreach (var source in Sources.ClassHookSources)
            {
                var sourceHooks = source.CollectBeforeClassHooks(string.Empty);
                foreach (var hook in sourceHooks)
                {
                    if (hook.MethodInfo?.Class?.Type != null && IsHookApplicableToClass(hook.MethodInfo.Class.Type, type))
                    {
                        var hookFunc = CreateClassHookDelegate(hook);
                        allHooks.Add((hook.Order, hookFunc));
                    }
                }
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

            foreach (var source in Sources.ClassHookSources)
            {
                var sourceHooks = source.CollectAfterClassHooks(string.Empty);
                foreach (var hook in sourceHooks)
                {
                    if (hook.MethodInfo?.Class?.Type != null && IsHookApplicableToClass(hook.MethodInfo.Class.Type, type))
                    {
                        var hookFunc = CreateClassHookDelegate(hook);
                        allHooks.Add((hook.Order, hookFunc));
                    }
                }
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

        foreach (var source in Sources.AssemblyHookSources)
        {
            var sourceHooks = source.CollectBeforeAssemblyHooks(string.Empty);
            foreach (var hook in sourceHooks)
            {
                try
                {
                    if (hook.Assembly != null && hook.Assembly == assembly)
                    {
                        var hookFunc = CreateAssemblyHookDelegate(hook);
                        allHooks.Add((hook.Order, hookFunc));
                    }
                }
                catch
                {
                    // Skip hooks with incomplete metadata
                }
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

        foreach (var source in Sources.AssemblyHookSources)
        {
            var sourceHooks = source.CollectAfterAssemblyHooks(string.Empty);
            foreach (var hook in sourceHooks)
            {
                try
                {
                    if (hook.Assembly != null && hook.Assembly == assembly)
                    {
                        var hookFunc = CreateAssemblyHookDelegate(hook);
                        allHooks.Add((hook.Order, hookFunc));
                    }
                }
                catch
                {
                    // Skip hooks with incomplete metadata
                }
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

        foreach (var source in Sources.TestSessionHookSources)
        {
            var sourceHooks = source.CollectBeforeTestSessionHooks(string.Empty);
            foreach (var hook in sourceHooks)
            {
                var hookFunc = CreateTestSessionHookDelegate(hook);
                allHooks.Add((hook.Order, hookFunc));
            }
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

        foreach (var source in Sources.TestSessionHookSources)
        {
            var sourceHooks = source.CollectAfterTestSessionHooks(string.Empty);
            foreach (var hook in sourceHooks)
            {
                var hookFunc = CreateTestSessionHookDelegate(hook);
                allHooks.Add((hook.Order, hookFunc));
            }
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

        foreach (var source in Sources.TestDiscoveryHookSources)
        {
            var sourceHooks = source.CollectBeforeTestDiscoveryHooks(string.Empty);
            foreach (var hook in sourceHooks)
            {
                var hookFunc = CreateBeforeTestDiscoveryHookDelegate(hook);
                allHooks.Add((hook.Order, hookFunc));
            }
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

        foreach (var source in Sources.TestDiscoveryHookSources)
        {
            var sourceHooks = source.CollectAfterTestDiscoveryHooks(string.Empty);
            foreach (var hook in sourceHooks)
            {
                var hookFunc = CreateTestDiscoveryHookDelegate(hook);
                allHooks.Add((hook.Order, hookFunc));
            }
        }

        var hooks = allHooks
            .OrderBy(h => h.order)
            .Select(h => h.hook)
            .ToList();

        return new ValueTask<IReadOnlyList<Func<TestDiscoveryContext, CancellationToken, Task>>>(hooks);
    }

    private static bool IsHookApplicableToClass(Type hookClassType, Type testClassType)
    {
        var currentType = testClassType;
        while (currentType != null)
        {
            if (currentType == hookClassType)
            {
                return true;
            }
            currentType = currentType.BaseType;
        }
        return false;
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

    private Dictionary<Type, List<(int order, InstanceHookMethod hook)>> BuildBeforeTestHooksIndex()
    {
        var index = new Dictionary<Type, List<(int, InstanceHookMethod)>>();

        foreach (var source in Sources.TestHookSources)
        {
            var hooks = source.CollectBeforeTestHooks(string.Empty);
            foreach (var hook in hooks)
            {
                if (!index.TryGetValue(hook.MethodInfo.Class.Type, out var list))
                {
                    list = [];
                    index[hook.MethodInfo.Class.Type] = list;
                }
                list.Add((hook.Order, hook));
            }
        }

        return index;
    }

    private Dictionary<Type, List<(int order, InstanceHookMethod hook)>> BuildAfterTestHooksIndex()
    {
        var index = new Dictionary<Type, List<(int, InstanceHookMethod)>>();

        foreach (var source in Sources.TestHookSources)
        {
            var hooks = source.CollectAfterTestHooks(string.Empty);
            foreach (var hook in hooks)
            {
                if (!index.TryGetValue(hook.MethodInfo.Class.Type, out var list))
                {
                    list = [];
                    index[hook.MethodInfo.Class.Type] = list;
                }
                list.Add((hook.Order, hook));
            }
        }

        return index;
    }
}
