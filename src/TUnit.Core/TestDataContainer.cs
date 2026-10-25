using System.Collections.Concurrent;
using System.Reflection;

namespace TUnit.Core;

internal static class TestDataContainer
{
    private static readonly object GlobalScope = new();

    private static readonly ConcurrentDictionary<InstanceKey, Lazy<object?>> _instances = new();

    // Disposable cached instances only, by identity, so that a disposed instance can be evicted.
    // Without eviction, a test registered after the last holder has disposed a shared instance
    // (e.g. a dynamic test) would be handed the disposed instance (#7204). A factory can return
    // the same instance for several keys or types, so every entry holding it is recorded.
    private static readonly ConcurrentDictionary<object, KeyValuePair<InstanceKey, Lazy<object?>>[]> _entriesByInstance =
        new(Helpers.ReferenceEqualityComparer.Instance);

    public static object? GetInstanceForClass(Type testClass, Type type, Func<Type, object> func)
    {
        return GetOrCreate(new InstanceKey(SharedType.PerClass, testClass, type), func);
    }

    public static object? GetInstanceForAssembly(Assembly assembly, Type type, Func<Type, object> func)
    {
        return GetOrCreate(new InstanceKey(SharedType.PerAssembly, assembly, type), func);
    }

    public static object? GetGlobalInstance(Type type, Func<Type, object> func)
    {
        return GetOrCreate(new InstanceKey(SharedType.PerTestSession, GlobalScope, type), func);
    }

    public static object? GetInstanceForKey(string key, Type type, Func<Type, object> func)
    {
        return GetOrCreate(new InstanceKey(SharedType.Keyed, key, type), func);
    }

    /// <summary>
    /// Whether <paramref name="instance"/> is a disposable instance that is still cached.
    /// </summary>
    public static bool IsCached(object instance) => _entriesByInstance.ContainsKey(instance);

    /// <summary>
    /// Removes <paramref name="instance"/> from the cache, so the next request for any scope and
    /// type it is cached under creates a new instance. Does nothing if it is not cached.
    /// </summary>
    public static void Evict(object instance)
    {
        if (_entriesByInstance.TryRemove(instance, out var entries))
        {
            foreach (var entry in entries)
            {
                // Removes the entry only while it still holds this instance's Lazy (reference equality).
                ((ICollection<KeyValuePair<InstanceKey, Lazy<object?>>>) _instances).Remove(entry);
            }
        }
    }

    /// <summary>
    /// Clears all cached shared instances. Called at the end of a run session so that a
    /// subsequent run request in the same process (e.g. IDE server mode) creates fresh
    /// instances instead of reusing already-disposed ones.
    /// </summary>
    public static void Reset()
    {
        _instances.Clear();
        _entriesByInstance.Clear();
    }

    private static object? GetOrCreate(InstanceKey key, Func<Type, object> func)
    {
        if (_instances.TryGetValue(key, out var existing))
        {
            return existing.Value;
        }

        return AddSlow(key, func);
    }

    // Kept separate from GetOrCreate so the fast path does not allocate the closure.
    private static object? AddSlow(InstanceKey key, Func<Type, object> func)
    {
        Lazy<object?> lazy = null!;
        lazy = new Lazy<object?>(() =>
        {
            var instance = func(key.Type);

            if (instance is IDisposable or IAsyncDisposable)
            {
                var entry = new KeyValuePair<InstanceKey, Lazy<object?>>(key, lazy);
                _entriesByInstance.AddOrUpdate(instance, _ => [entry], (_, entries) => [..entries, entry]);
            }

            return instance;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        return _instances.GetOrAdd(key, lazy).Value;
    }

    private readonly struct InstanceKey(SharedType kind, object scope, Type type) : IEquatable<InstanceKey>
    {
        public SharedType Kind { get; } = kind;

        public object Scope { get; } = scope;

        public Type Type { get; } = type;

        public bool Equals(InstanceKey other) => Kind == other.Kind && Type == other.Type && Scope.Equals(other.Scope);

        public override bool Equals(object? obj) => obj is InstanceKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Kind * 397 ^ Scope.GetHashCode()) * 397 ^ Type.GetHashCode();
            }
        }
    }
}
