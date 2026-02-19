using System.Collections.Concurrent;
using System.Reflection;

namespace TUnit.Core.AotCompatibility;

/// <summary>
/// Registry for pre-compiled generic test method instances to support AOT scenarios.
/// Populated by source generator to avoid MakeGenericMethod at runtime.
/// </summary>
public static class GenericTestRegistry
{
    private static readonly ConcurrentDictionary<GenericMethodKey, MethodInfo> _compiledMethods = new();
    private static readonly ConcurrentDictionary<Type, HashSet<Type[]>> _registeredCombinations = new();
    private static readonly ConcurrentDictionary<GenericMethodKey, Delegate> _directInvocationDelegates = new();
    private static readonly object _combinationsLock = new();

    /// <summary>
    /// Registers a pre-compiled generic method instance.
    /// Called by source-generated module initializers.
    /// </summary>
    public static void RegisterGenericMethod(Type declaringType, string methodName, Type[] typeArguments, MethodInfo compiledMethod)
    {
        var key = new GenericMethodKey(declaringType, methodName, typeArguments);
        _compiledMethods[key] = compiledMethod;

        // Track registered combinations - lock protects the inner HashSet from concurrent modification
        var combinations = _registeredCombinations.GetOrAdd(declaringType, static _ => new HashSet<Type[]>(new TypeArrayComparer()));
        lock (_combinationsLock)
        {
            combinations.Add(typeArguments);
        }
    }

    /// <summary>
    /// Registers a direct invocation delegate for a generic method.
    /// Avoids all reflection at runtime.
    /// </summary>
    public static void RegisterDirectDelegate<T>(Type declaringType, string methodName, Type[] typeArguments, T delegateInstance)
        where T : Delegate
    {
        var key = new GenericMethodKey(declaringType, methodName, typeArguments);
        _directInvocationDelegates[key] = delegateInstance;
    }

    /// <summary>
    /// Gets a pre-compiled generic method instance if available.
    /// Returns null if not registered (fallback to reflection needed).
    /// </summary>
    public static MethodInfo? GetCompiledMethod(Type declaringType, string methodName, Type[] typeArguments)
    {
        var key = new GenericMethodKey(declaringType, methodName, typeArguments);
        return _compiledMethods.TryGetValue(key, out var method) ? method : null;
    }

    /// <summary>
    /// Gets a direct invocation delegate if available.
    /// Fastest path - no reflection at all.
    /// </summary>
    public static TDelegate? GetDirectDelegate<TDelegate>(Type declaringType, string methodName, Type[] typeArguments)
        where TDelegate : Delegate
    {
        var key = new GenericMethodKey(declaringType, methodName, typeArguments);
        if (_directInvocationDelegates.TryGetValue(key, out var del))
        {
            return del as TDelegate;
        }
        return null;
    }

    /// <summary>
    /// Checks if a generic combination is registered for AOT.
    /// </summary>
    public static bool IsRegistered(Type declaringType, string methodName, Type[] typeArguments)
    {
        var key = new GenericMethodKey(declaringType, methodName, typeArguments);
        return _compiledMethods.ContainsKey(key) || _directInvocationDelegates.ContainsKey(key);
    }

    /// <summary>
    /// Gets all registered type combinations for a declaring type.
    /// </summary>
    public static IEnumerable<Type[]> GetRegisteredCombinations(Type declaringType)
    {
        if (!_registeredCombinations.TryGetValue(declaringType, out var combinations))
        {
            return Array.Empty<Type[]>();
        }

        // Return a snapshot to avoid enumeration during concurrent modification
        lock (_combinationsLock)
        {
            return combinations.ToArray();
        }
    }

    /// <summary>
    /// Marks a test method as AOT-compatible after registration.
    /// </summary>
    public static void MarkAsAotCompatible(MethodInfo method)
    {
        // This information can be used by the analyzer to suppress warnings
        AotCompatibleMethods.TryAdd(method, 0);
    }

    private static readonly ConcurrentDictionary<MethodInfo, byte> AotCompatibleMethods = new();

    /// <summary>
    /// Checks if a method has been marked as AOT-compatible.
    /// </summary>
    public static bool IsMarkedAotCompatible(MethodInfo method)
    {
        return AotCompatibleMethods.ContainsKey(method);
    }

    /// <summary>
    /// Clears all registrations. Useful for testing.
    /// </summary>
    internal static void Clear()
    {
        _compiledMethods.Clear();
        _registeredCombinations.Clear();
        _directInvocationDelegates.Clear();
        AotCompatibleMethods.Clear();
    }

    private record struct GenericMethodKey(Type DeclaringType, string MethodName, Type[] TypeArguments)
    {
        public readonly bool Equals(GenericMethodKey other)
        {
            return DeclaringType == other.DeclaringType &&
                   MethodName == other.MethodName &&
                   TypeArrayComparer.Instance.Equals(TypeArguments, other.TypeArguments);
        }

        public override readonly int GetHashCode()
        {
#if NETSTANDARD2_0
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (DeclaringType?.GetHashCode() ?? 0);
                hash = hash * 31 + (MethodName?.GetHashCode() ?? 0);
                foreach (var type in TypeArguments)
                {
                    hash = hash * 31 + (type?.GetHashCode() ?? 0);
                }
                return hash;
            }
#else
            var hash = HashCode.Combine(DeclaringType, MethodName);
            foreach (var type in TypeArguments)
            {
                hash = HashCode.Combine(hash, type);
            }
            return hash;
#endif
        }
    }

    private class TypeArrayComparer : IEqualityComparer<Type[]>
    {
        public static readonly TypeArrayComparer Instance = new();

        public bool Equals(Type[]? x, Type[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.Length != y.Length) return false;

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i]) return false;
            }

            return true;
        }

        public int GetHashCode(Type[] obj)
        {
#if NETSTANDARD2_0
            unchecked
            {
                var hash = obj.Length;
                foreach (var type in obj)
                {
                    hash = hash * 31 + (type?.GetHashCode() ?? 0);
                }
                return hash;
            }
#else
            var hash = obj.Length;
            foreach (var type in obj)
            {
                hash = HashCode.Combine(hash, type);
            }
            return hash;
#endif
        }
    }
}

/// <summary>
/// Attribute to mark methods as AOT-compatible.
/// Applied by source generator or manually.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AotCompatibleAttribute : Attribute
{
    /// <summary>
    /// How AOT compatibility was achieved.
    /// </summary>
    public AotCompatibilityMode Mode { get; init; }

    /// <summary>
    /// Additional information about the registration.
    /// </summary>
    public string? Details { get; init; }
}

public enum AotCompatibilityMode
{
    /// <summary>
    /// Source generator registered all combinations.
    /// </summary>
    SourceGenerated,

    /// <summary>
    /// Manually registered in module initializer.
    /// </summary>
    ManuallyRegistered,

    /// <summary>
    /// Uses only concrete types, no generics.
    /// </summary>
    ConcreteTypesOnly,

    /// <summary>
    /// Uses ITuple interface instead of reflection.
    /// </summary>
    TupleInterfaceBased
}