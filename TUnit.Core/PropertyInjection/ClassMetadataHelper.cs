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
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Metadata creation")]
    public static ClassMetadata GetOrCreateClassMetadata(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors | 
            DynamicallyAccessedMemberTypes.NonPublicConstructors | 
            DynamicallyAccessedMemberTypes.PublicMethods | 
            DynamicallyAccessedMemberTypes.NonPublicMethods | 
            DynamicallyAccessedMemberTypes.PublicProperties)] 
        Type type)
    {
        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () =>
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var constructor = constructors.FirstOrDefault();

            var constructorParameters = constructor?.GetParameters().Select((p, i) => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? $"param{i}",
                TypeReference = new TypeReference { AssemblyQualifiedName = p.ParameterType.AssemblyQualifiedName },
                ReflectionInfo = p
            }).ToArray() ?? Array.Empty<ParameterMetadata>();

            return new ClassMetadata
            {
                Type = type,
                TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
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
}