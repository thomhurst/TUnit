using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Core;

namespace TUnit.Engine.Building;

internal static class ReflectionMetadataBuilder
{
    /// <summary>
    /// Creates method metadata from reflection info with proper ReflectionInfo populated
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Method metadata creation uses reflection on parameters and types")]
#endif
    public static MethodMetadata CreateMethodMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.NonPublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods
            | DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        System.Reflection.MethodInfo method)
    {
        return new MethodMetadata
        {
            Name = method.Name,
            Type = type,
            TypeReference = CreateTypeReference(type),
            Class = CreateClassMetadata(type),
            Parameters = method.GetParameters()
                .Select((p, i) => CreateParameterMetadata(p.ParameterType, p.Name ?? "unnamed", i, p))
                .ToArray(),
            GenericTypeCount = method.IsGenericMethodDefinition ? method.GetGenericArguments().Length : 0,
            ReturnTypeReference = CreateTypeReference(method.ReturnType),
            ReturnType = method.ReturnType
        };
    }

    private static TypeReference CreateTypeReference(Type type)
    {
        return TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
    }

#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Parameter metadata creation uses reflection")]
#endif
    private static ParameterMetadata CreateParameterMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.PublicProperties)]Type parameterType, string? name, int index, System.Reflection.ParameterInfo reflectionInfo)
    {
        return new ParameterMetadata(parameterType)
        {
            Name = name ?? $"param{index}",
            TypeReference = CreateTypeReference(parameterType),
            ReflectionInfo = reflectionInfo,
            Type = parameterType,
            IsNullable = parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>)
                || Nullable.GetUnderlyingType(parameterType) != null
        };
    }

#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Class metadata creation uses reflection on constructors")]
#endif
    private static ClassMetadata CreateClassMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
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
                Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.GetName().FullName, () => new AssemblyMetadata
                {
                    Name = type.Assembly.GetName().Name ?? "Unknown"
                }),
                Parameters = constructorParameters,
                Properties = [],
                Parent = null
            };
        });
    }
}
