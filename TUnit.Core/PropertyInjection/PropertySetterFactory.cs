using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Factory for creating property setters with caching for performance.
/// Consolidates all property setter creation logic in one place following DRY principle.
/// </summary>
/// <remarks>
/// Setters are cached using the PropertyInfo as the key to avoid repeated reflection calls.
/// This significantly improves performance when the same property is accessed multiple times
/// (e.g., in test retries or shared test data scenarios).
/// </remarks>
internal static class PropertySetterFactory
{
    // Cache setters per PropertyInfo to avoid repeated reflection
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> SetterCache = new();

    /// <summary>
    /// Gets or creates a setter delegate for the given property.
    /// Uses caching to avoid repeated reflection calls.
    /// </summary>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Backing field access for init-only properties requires reflection")]
    #endif
    public static Action<object, object?> GetOrCreateSetter(PropertyInfo property)
    {
        return SetterCache.GetOrAdd(property, CreateSetterCore);
    }

    /// <summary>
    /// Creates a setter delegate for the given property.
    /// Consider using <see cref="GetOrCreateSetter"/> for better performance through caching.
    /// </summary>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Backing field access for init-only properties requires reflection")]
    #endif
    public static Action<object, object?> CreateSetter(PropertyInfo property)
    {
        // Delegate to cached version for consistency
        return GetOrCreateSetter(property);
    }

    /// <summary>
    /// Core implementation for creating a setter delegate.
    /// Called by GetOrCreateSetter for caching.
    /// </summary>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Backing field access for init-only properties requires reflection")]
    #endif
    private static Action<object, object?> CreateSetterCore(PropertyInfo property)
    {
        if (property.CanWrite && property.SetMethod != null)
        {
#if NETSTANDARD2_0
            return (instance, value) => property.SetValue(instance, value);
#else
            var setMethod = property.SetMethod;
            var isInitOnly = IsInitOnlyMethod(setMethod);

            if (!isInitOnly)
            {
                return (instance, value) => property.SetValue(instance, value);
            }
#endif
        }

        var backingField = GetBackingField(property);
        if (backingField != null)
        {
            return (instance, value) => backingField.SetValue(instance, value);
        }

        throw new InvalidOperationException(
            $"Property '{property.Name}' on type '{property.DeclaringType?.Name}' " +
            $"is not writable and no backing field was found.");
    }

    /// <summary>
    /// Gets the backing field for a property.
    /// </summary>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Backing field access for init-only properties requires reflection")]
    #endif
    private static FieldInfo? GetBackingField(PropertyInfo property)
    {
        var declaringType = property.DeclaringType;
        if (declaringType == null)
        {
            return null;
        }

        var backingFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        // Try compiler-generated backing field name
        var backingFieldName = $"<{property.Name}>k__BackingField";
        var field = GetFieldSafe(declaringType, backingFieldName, backingFieldFlags);
        if (field != null)
        {
            return field;
        }

        // Try underscore-prefixed camelCase name
#if NET8_0_OR_GREATER
        Span<char> buffer = stackalloc char[property.Name.Length + 1];
        buffer[0] = '_';
        buffer[1] = char.ToLowerInvariant(property.Name[0]);
        property.Name.AsSpan(1).CopyTo(buffer.Slice(2));
        var underscoreName = new string(buffer);
#else
        var underscoreName = "_" + char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
#endif
        field = GetFieldSafe(declaringType, underscoreName, backingFieldFlags);
        if (field != null && field.FieldType == property.PropertyType)
        {
            return field;
        }

        // Try exact property name
        field = GetFieldSafe(declaringType, property.Name, backingFieldFlags);
        if (field != null && field.FieldType == property.PropertyType)
        {
            return field;
        }

        return null;
    }

    /// <summary>
    /// Helper method to get field with proper trimming suppression.
    /// </summary>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Field access for property backing fields requires reflection")]
    #endif
    private static FieldInfo? GetFieldSafe(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        Type type,
        string name,
        BindingFlags bindingFlags)
    {
        return type.GetField(name, bindingFlags);
    }

    /// <summary>
    /// Checks if a method is init-only.
    /// </summary>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Checking for init-only setters requires reflection")]
    #endif
    private static bool IsInitOnlyMethod(MethodInfo setMethod)
    {
        var methodType = setMethod.GetType();
        var isInitOnlyProperty = methodType.GetProperty("IsInitOnly");
        return isInitOnlyProperty != null && (bool)isInitOnlyProperty.GetValue(setMethod)!;
    }
}
