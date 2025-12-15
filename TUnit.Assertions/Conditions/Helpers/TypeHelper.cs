using System.Collections.Concurrent;

namespace TUnit.Assertions.Conditions.Helpers;

/// <summary>
/// Helper methods for type checking and classification.
/// Consolidates type checking logic to ensure consistent behavior across assertion classes.
/// </summary>
internal static class TypeHelper
{
    /// <summary>
    /// Thread-safe registry of user-defined types that should be treated as primitives
    /// (using value equality rather than structural comparison).
    /// </summary>
    private static readonly ConcurrentDictionary<Type, byte> CustomPrimitiveTypes = new();

    /// <summary>
    /// Registers a type to be treated as a primitive for structural equivalency comparisons.
    /// Once registered, instances of this type will use value equality (via Equals) rather
    /// than having their properties compared individually.
    /// </summary>
    /// <typeparam name="T">The type to register as a primitive.</typeparam>
    public static void RegisterAsPrimitive<T>()
    {
        CustomPrimitiveTypes.TryAdd(typeof(T), 0);
    }

    /// <summary>
    /// Registers a type to be treated as a primitive for structural equivalency comparisons.
    /// </summary>
    /// <param name="type">The type to register as a primitive.</param>
    public static void RegisterAsPrimitive(Type type)
    {
        CustomPrimitiveTypes.TryAdd(type, 0);
    }

    /// <summary>
    /// Removes a previously registered custom primitive type.
    /// </summary>
    /// <typeparam name="T">The type to unregister.</typeparam>
    /// <returns>True if the type was removed; false if it wasn't registered.</returns>
    public static bool UnregisterPrimitive<T>()
    {
        return CustomPrimitiveTypes.TryRemove(typeof(T), out _);
    }

    /// <summary>
    /// Clears all registered custom primitive types.
    /// Useful for test cleanup between tests.
    /// </summary>
    public static void ClearCustomPrimitives()
    {
        CustomPrimitiveTypes.Clear();
    }

    /// <summary>
    /// Determines if a type is a primitive or well-known immutable type that should use
    /// value equality rather than structural comparison.
    /// Types implementing IEquatable&lt;T&gt; are also considered primitive-like for comparison purposes.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type should use value equality; false for structural comparison.</returns>
    public static bool IsPrimitiveOrWellKnownType(
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
        Type type)
    {
        // Check user-defined primitives first (fast path for common case)
        if (CustomPrimitiveTypes.ContainsKey(type))
        {
            return true;
        }

        // Check if type is a well-known primitive type
        if (type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
#if NET6_0_OR_GREATER
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
#endif
            )
        {
            return true;
        }

        // Check if type implements IEquatable<T> for its own type
        // Types implementing IEquatable<T> should use their Equals method
        // rather than structural comparison (e.g., Vector2, Uri, CultureInfo)
        var equatableInterface = type.GetInterface("IEquatable`1");
        if (equatableInterface != null)
        {
            var genericArgs = equatableInterface.GetGenericArguments();
            if (genericArgs.Length == 1 && genericArgs[0] == type)
            {
                return true;
            }
        }

        return false;
    }
}
