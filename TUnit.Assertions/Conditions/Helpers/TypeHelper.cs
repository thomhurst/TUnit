using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

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
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type should use value equality; false for structural comparison.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "This method is only called from code paths that already require reflection (StructuralEqualityComparer)")]
    public static bool IsPrimitiveOrWellKnownType(Type type)
    {
        // Check user-defined primitives first (fast path for common case)
        if (CustomPrimitiveTypes.ContainsKey(type))
        {
            return true;
        }

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

        // Check if the type is a value type (struct) that implements IEquatable<T> for itself
        // Value types like Vector2, Matrix3x2, etc. that implement IEquatable<T>
        // should use value equality rather than structural comparison.
        // We only check value types to avoid affecting records/classes that may have
        // collection properties requiring structural comparison.
        if (type.IsValueType && ImplementsSelfEquatable(type))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a type implements IEquatable{T} where T is the type itself.
    /// </summary>
    private static bool ImplementsSelfEquatable(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type type)
    {
        // Iterate through interfaces to find IEquatable<T> where T is the type itself
        // This approach is AOT-compatible as it doesn't use MakeGenericType
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType 
                && iface.GetGenericTypeDefinition() == typeof(IEquatable<>)
                && iface.GenericTypeArguments[0] == type)
            {
                return true;
            }
        }
        
        return false;
    }
}
