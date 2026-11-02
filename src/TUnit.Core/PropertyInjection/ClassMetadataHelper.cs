using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Helper class for creating and managing ClassMetadata instances.
/// Follows DRY principle by consolidating ClassMetadata creation logic.
/// </summary>
internal static class ClassMetadataHelper
{
    /// <summary>
    /// Gets or creates ClassMetadata for the specified type.
    /// </summary>
    #if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Metadata creation requires reflection")]
    #endif
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

            return new ClassMetadata
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
            };
        });
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
