using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Factory for creating property setters.
/// Consolidates all property setter creation logic in one place following DRY principle.
/// </summary>
internal static class PropertySetterFactory
{
    /// <summary>
    /// Creates a setter delegate for the given property.
    /// </summary>
    public static Action<object, object?> CreateSetter(PropertyInfo property)
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
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Accessing backing fields for init-only and required properties in reflection mode. The compiler-generated field naming pattern (<property>k__BackingField) is stable. For AOT, source generation creates direct setters.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "PropertyInfo.DeclaringType may not carry field annotations, but we only access compiler-generated backing fields which are preserved when the property is preserved. For AOT, use source-generated setters.")]
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
        var underscoreName = "_" + char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
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
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Safe field access when DynamicallyAccessedMembers annotations are present. The caller ensures the type has the required field preservation. This is only used for setting property backing fields in reflection mode.")]
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
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Checking for System.Runtime.CompilerServices.IsExternalInit modreq to identify init-only setters. This is a stable .NET runtime convention. For AOT, the source generator identifies init-only properties at compile time and generates appropriate setters.")]
    private static bool IsInitOnlyMethod(MethodInfo setMethod)
    {
        var methodType = setMethod.GetType();
        var isInitOnlyProperty = methodType.GetProperty("IsInitOnly");
        return isInitOnlyProperty != null && (bool)isInitOnlyProperty.GetValue(setMethod)!;
    }
}