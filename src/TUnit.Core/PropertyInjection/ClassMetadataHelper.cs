using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Helper class for creating and managing ClassMetadata instances.
/// Follows DRY principle by consolidating ClassMetadata creation logic.
/// </summary>
internal static class ClassMetadataHelper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new();

    /// <summary>
    /// Gets or creates ClassMetadata for the specified type.
    /// </summary>
    [RequiresUnreferencedCode("Metadata creation requires reflection")]
    public static ClassMetadata GetOrCreateClassMetadata(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type)
    {
        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () =>
        {
            var constructor = GetParameterConstructor(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

            var constructorParameters = constructor?.GetParameters().Select((p, i) => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? $"param{i}",
                TypeInfo = new ConcreteType(p.ParameterType),
                ReflectionInfo = p
            }).ToArray() ?? [];

            return WithProperties(new ClassMetadata
            {
                Type = type,
                TypeInfo = new ConcreteType(type),
                Name = type.Name,
                Namespace = type.Namespace ?? string.Empty,
                Assembly = AssemblyMetadata.GetOrAdd(
                    type.Assembly.GetName().Name ?? type.Assembly.GetName().FullName ?? "Unknown", 
                    () => new AssemblyMetadata
                    {
                        Name = type.Assembly.GetName().Name ?? type.Assembly.GetName().FullName ?? "Unknown"
                    }),
                Properties = [],
                Parameters = constructorParameters,
                Parent = type.DeclaringType != null ? GetOrCreateClassMetadata(type.DeclaringType) : null
            });
        });
    }

    // Match generated metadata: properties declared on this type, including static properties.
    // Called inside the class cache factory, so back-references never recurse through that cache.
    [RequiresUnreferencedCode("Property metadata creation requires reflection")]
    public static ClassMetadata WithProperties(ClassMetadata metadata)
    {
        if (!Properties.TryGetValue(metadata.Type, out var properties))
        {
            properties = metadata.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(p => p.Name != "EqualityContract").ToArray();
            properties = Properties.GetOrAdd(metadata.Type, properties);
        }
        var result = metadata with
        {
            Properties = properties.Select(p => new PropertyMetadata
            {
                Name = p.Name,
                Type = p.PropertyType,
                ReflectionInfo = p,
                IsStatic = (p.GetMethod ?? p.SetMethod)!.IsStatic,
                IsNullable = IsNullable(p),
                Getter = instance => p.GetValue(instance),
                ClassMetadata = null!,
                ContainingTypeMetadata = null!
            }).ToArray()
        };
        foreach (var property in result.Properties)
        {
            property.ClassMetadata = result;
            property.ContainingTypeMetadata = result;
        }
        return result;
    }

    // Read compiler annotations directly so the netstandard build has the same behavior
    // without adding a dependency on NullabilityInfoContext (unavailable on that target).
    private static bool IsNullable(PropertyInfo property)
    {
        if (property.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) != null;
        }

        var annotation = GetNullableFlag(property.GetCustomAttributesData(), "System.Runtime.CompilerServices.NullableAttribute");
        if (annotation != null)
        {
            return annotation == 2;
        }

        if (property.GetMethod != null)
        {
            annotation = GetNullableFlag(property.GetMethod.GetCustomAttributesData(), "System.Runtime.CompilerServices.NullableContextAttribute");
        }
        for (var type = property.DeclaringType; annotation == null && type != null; type = type.DeclaringType)
        {
            annotation = GetNullableFlag(type.GetCustomAttributesData(), "System.Runtime.CompilerServices.NullableContextAttribute");
        }
        return annotation == 2;
    }

    private static byte? GetNullableFlag(IList<CustomAttributeData> attributes, string attributeName)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeType.FullName != attributeName || attribute.ConstructorArguments.Count != 1)
            {
                continue;
            }
            var value = attribute.ConstructorArguments[0].Value;
            if (value is byte flag)
            {
                return flag;
            }
            if (value is IList<CustomAttributeTypedArgument> flags && flags.Count > 0 && flags[0].Value is byte first)
            {
                return first;
            }
        }
        return null;
    }

    /// <summary>
    /// The public constructor whose parameters describe a class's <see cref="ClassMetadata.Parameters"/>: the one
    /// marked <c>[TestConstructor]</c>, which the test instance is built with, otherwise the first one. Every
    /// reflection-mode builder of <see cref="ClassMetadata"/> shares one cache entry per type, so they must agree.
    /// </summary>
    public static ConstructorInfo? GetParameterConstructor(ConstructorInfo[] publicConstructors)
    {
        foreach (var constructor in publicConstructors)
        {
            if (constructor.IsDefined(typeof(TestConstructorAttribute), inherit: false))
            {
                return constructor;
            }
        }

        return publicConstructors.Length > 0 ? publicConstructors[0] : null;
    }
}
