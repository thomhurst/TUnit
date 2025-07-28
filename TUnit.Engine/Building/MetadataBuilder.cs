using System.Diagnostics.CodeAnalysis;
using TUnit.Core;

namespace TUnit.Engine.Building;

internal static class MetadataBuilder
{
    [UnconditionalSuppressMessage("AOT", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ClassMetadata.Type.init'", Justification = "Type annotations are handled by source generators")]
    [UnconditionalSuppressMessage("AOT", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' in call to 'System.Type.GetConstructors(BindingFlags)'", Justification = "Constructor discovery needed for metadata")]
    public static ClassMetadata CreateClassMetadata(TestMetadata metadata)
    {
        var type = metadata.TestClassType;

        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () => 
        {
            // Get constructor parameters for the class
            var constructors = type.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var constructor = constructors.FirstOrDefault();
            
            var constructorParameters = constructor?.GetParameters().Select((p, i) => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? $"param{i}",
                TypeReference = new TypeReference { AssemblyQualifiedName = p.ParameterType.AssemblyQualifiedName },
                ReflectionInfo = p
            }).ToArray() ?? [];
            
            return new ClassMetadata
            {
                Name = type.Name,
                Type = type,
                TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
                Namespace = type.Namespace,
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

    [UnconditionalSuppressMessage("AOT", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.MethodMetadata.Type.init'", Justification = "Type annotations are handled by source generators")]
    [UnconditionalSuppressMessage("AOT", "IL2067:'Type' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ParameterMetadata.ParameterMetadata(Type)'", Justification = "Parameter types are known at compile time")]
    public static MethodMetadata CreateMethodMetadata(TestMetadata metadata)
    {
        var parameters = metadata.ParameterTypes.Select((type, index) => new ParameterMetadata(type)
        {
            Name = $"param{index}",
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
            ReflectionInfo = null!
        }).ToArray();

        return new MethodMetadata
        {
            Name = metadata.TestMethodName,
            Type = metadata.TestClassType,
            TypeReference = TypeReference.CreateConcrete(metadata.TestClassType.AssemblyQualifiedName ?? metadata.TestClassType.FullName ?? metadata.TestClassType.Name),
            Class = CreateClassMetadata(metadata),
            Parameters = parameters,
            GenericTypeCount = 0,
            ReturnTypeReference = TypeReference.CreateConcrete(typeof(Task).AssemblyQualifiedName ?? typeof(Task).FullName ?? "System.Threading.Tasks.Task"),
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
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
            Class = ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () => 
            {
                // Get constructor parameters for the class
                var constructors = type.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var constructor = constructors.FirstOrDefault();
                
                var constructorParameters = constructor?.GetParameters().Select((p, i) => new ParameterMetadata(p.ParameterType)
                {
                    Name = p.Name ?? $"param{i}",
                    TypeReference = new TypeReference { AssemblyQualifiedName = p.ParameterType.AssemblyQualifiedName },
                    ReflectionInfo = p
                }).ToArray() ?? [];
                
                return new ClassMetadata
                {
                    Name = type.Name,
                    Type = type,
                    TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
                    Namespace = type.Namespace ?? string.Empty,
                    Assembly = new AssemblyMetadata { Name = type.Assembly.GetName().Name ?? "Unknown" },
                    Parameters = constructorParameters,
                    Properties = [],
                    Parent = null
                };
            }),
            Parameters = method.GetParameters().Select(p => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? "unnamed",
                TypeReference = TypeReference.CreateConcrete(p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.FullName ?? p.ParameterType.Name),
                ReflectionInfo = p
            }).ToArray(),
            GenericTypeCount = method.IsGenericMethodDefinition ? method.GetGenericArguments().Length : 0,
            ReturnTypeReference = TypeReference.CreateConcrete(method.ReturnType.AssemblyQualifiedName ?? method.ReturnType.FullName ?? method.ReturnType.Name),
            ReturnType = method.ReturnType,
        };
    }
}
