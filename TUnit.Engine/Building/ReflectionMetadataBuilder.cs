using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Core;

namespace TUnit.Engine.Building;

internal static class ReflectionMetadataBuilder
{
    /// <summary>
    /// Creates method metadata from reflection info with proper ReflectionInfo populated
    /// </summary>
#if NET8_0_OR_GREATER
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
            TypeInfo = CreateTypeInfo(type),
            Class = CreateClassMetadata(type),
            Parameters = ParameterMetadataFactory.Build(method.GetParameters(), nameFallback: "unnamed", computeIsNullable: true),
            GenericTypeCount = method.IsGenericMethodDefinition ? method.GetGenericArguments().Length : 0,
            ReturnTypeInfo = CreateTypeInfo(method.ReturnType),
            ReturnType = method.ReturnType
        };
    }

    private static TypeInfo CreateTypeInfo(Type type)
    {
        return new ConcreteType(type);
    }

#if NET8_0_OR_GREATER
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

            // Constructor params pass a null fallback so a null ParameterInfo.Name yields
            // "param{index}" (not "unnamed"), preserving the original per-callsite behaviour.
            var constructorParameters = constructor is null
                ? []
                : ParameterMetadataFactory.Build(constructor.GetParameters(), nameFallback: null, computeIsNullable: true);

            return new ClassMetadata
            {
                Name = type.Name,
                Type = type,
                TypeInfo = CreateTypeInfo(type),
                Namespace = type.Namespace ?? string.Empty,
                Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.GetName().FullName, () => new AssemblyMetadata
                {
                    Name = type.Assembly.GetName().Name ?? "Unknown"
                }),
                Parameters = constructorParameters,
                Properties = [],
                Parent = type.DeclaringType != null ? CreateClassMetadata(type.DeclaringType) : null
            };
        });
    }
}
