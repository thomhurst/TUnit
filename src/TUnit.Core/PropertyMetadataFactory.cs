using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

/// <summary>
/// Creates property accessors for source-generated metadata.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PropertyMetadataFactory
{
    /// <summary>
    /// Creates a cached reflection getter for an instance property on an open generic declaring type.
    /// Each closed declaring type is resolved from the instance without runtime code generation.
    /// </summary>
    public static Func<object?, object?> CreateGenericGetter(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type declaringType,
        string propertyName)
    {
        var properties = new ConcurrentDictionary<Type, PropertyInfo>();
        Func<Type, PropertyInfo> resolveProperty = ResolveProperty;
        return instance =>
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            var property = properties.GetOrAdd(instance.GetType(), resolveProperty);
            return property.GetValue(instance);
        };

        [UnconditionalSuppressMessage("Trimming", "IL2070",
            Justification = "The matching closed type is an instantiation of declaringType, whose public properties are preserved by the annotated parameter.")]
        [UnconditionalSuppressMessage("Trimming", "IL2075",
            Justification = "Base types are only inspected until the preserved generic declaring type is found; no properties on other types are accessed.")]
        PropertyInfo ResolveProperty(Type instanceType)
        {
            for (var type = instanceType; type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == declaringType)
                {
                    return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        ?? throw new MissingMemberException(type.FullName, propertyName);
                }
            }

            throw new ArgumentException($"Type '{instanceType}' does not inherit from '{declaringType}'.", "instance");
        }
    }
}
