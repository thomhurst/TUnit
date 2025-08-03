using System.Diagnostics.CodeAnalysis;
using TUnit.Core;

namespace TUnit.Engine.Building;

internal static class MetadataBuilder
{
    private static TypeReference CreateTypeReference(Type type)
    {
        return TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
    }

    [UnconditionalSuppressMessage("AOT", "IL2067:'Type' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ParameterMetadata.ParameterMetadata(Type)'", Justification = "Parameter types are known at compile time")]
    private static ParameterMetadata CreateParameterMetadata(Type parameterType, string? name, int index, System.Reflection.ParameterInfo? reflectionInfo = null)
    {
        return new ParameterMetadata(parameterType)
        {
            Name = name ?? $"param{index}",
            TypeReference = CreateTypeReference(parameterType),
            ReflectionInfo = reflectionInfo
        };
    }
    [UnconditionalSuppressMessage("AOT", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ClassMetadata.Type.init'", Justification = "Type annotations are handled by source generators")]
    [UnconditionalSuppressMessage("AOT", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' in call to 'System.Type.GetConstructors(BindingFlags)'", Justification = "Constructor discovery needed for metadata")]
    private static ClassMetadata CreateClassMetadataInternal(Type type)
    {
        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () => 
        {
            // Get constructor parameters for the class
            var constructors = type.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var constructor = constructors.FirstOrDefault();
            
            var constructorParameters = constructor?.GetParameters()
                .Select((p, i) => CreateParameterMetadata(p.ParameterType, p.Name, i, p))
                .ToArray() ?? [];
            
            return new ClassMetadata
            {
                Name = type.Name,
                Type = type,
                TypeReference = CreateTypeReference(type),
                Namespace = type.Namespace ?? string.Empty,
                Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.FullName ?? "Unknown", () => new AssemblyMetadata
                {
                    Name = type.Assembly.GetName().Name ?? "Unknown",
                }),
                Parameters = constructorParameters,
                Properties = [],
                Parent = null,
            };
        });
    }

    public static ClassMetadata CreateClassMetadata(TestMetadata metadata)
    {
        return CreateClassMetadataInternal(metadata.TestClassType);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.MethodMetadata.Type.init'", Justification = "Type annotations are handled by source generators")]
    [UnconditionalSuppressMessage("AOT", "IL2067:'Type' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ParameterMetadata.ParameterMetadata(Type)'", Justification = "Parameter types are known at compile time")]
    public static MethodMetadata CreateMethodMetadata(TestMetadata metadata)
    {
        var parameters = metadata.ParameterTypes
            .Select((type, index) => CreateParameterMetadata(type, null, index))
            .ToArray();

        return new MethodMetadata
        {
            Name = metadata.TestMethodName,
            Type = metadata.TestClassType,
            TypeReference = CreateTypeReference(metadata.TestClassType),
            Class = CreateClassMetadata(metadata),
            Parameters = parameters,
            GenericTypeCount = 0,
            ReturnTypeReference = CreateTypeReference(typeof(Task)),
            ReturnType = typeof(Task),
        };
    }

    /// <summary>
    /// Creates method metadata from reflection info with proper ReflectionInfo populated
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.MethodMetadata.Type.init'", Justification = "Type annotations are handled by reflection")]
    [UnconditionalSuppressMessage("AOT", "IL2067:'Type' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ParameterMetadata.ParameterMetadata(Type)'", Justification = "Parameter types are known through reflection")]
    [UnconditionalSuppressMessage("AOT", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' in call to 'System.Type.GetConstructors(BindingFlags)'", Justification = "Constructor discovery needed for metadata")]
    public static MethodMetadata CreateMethodMetadata(Type type, System.Reflection.MethodInfo method)
    {
        return new MethodMetadata
        {
            Name = method.Name,
            Type = type,
            TypeReference = CreateTypeReference(type),
            Class = CreateClassMetadataInternal(type),
            Parameters = method.GetParameters()
                .Select((p, i) => CreateParameterMetadata(p.ParameterType, p.Name ?? "unnamed", i, p))
                .ToArray(),
            GenericTypeCount = method.IsGenericMethodDefinition ? method.GetGenericArguments().Length : 0,
            ReturnTypeReference = CreateTypeReference(method.ReturnType),
            ReturnType = method.ReturnType,
        };
    }
}
