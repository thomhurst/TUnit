using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Building;

/// <summary>
/// Builds <see cref="ParameterMetadata"/> arrays from reflection <see cref="ParameterInfo"/>s for the
/// reflection-mode runtime fallback. Shared by <see cref="ReflectionMetadataBuilder"/> (test methods /
/// constructors) and the reflection hook discovery service so the two paths cannot drift.
///
/// Call sites differ only in two observable details, which are exposed as parameters here so the
/// original per-callsite behaviour is preserved exactly:
///  - <paramref name="nameFallback"/> — what a null <see cref="ParameterInfo.Name"/> collapses to before
///    the final "param{index}" fallback. Method params used "unnamed"; constructor params used null
///    (=> "param{index}"); hook params used string.Empty.
///  - <paramref name="computeIsNullable"/> — only the method/constructor metadata path computed IsNullable;
///    the hook path left it at its default (false).
/// </summary>
internal static class ParameterMetadataFactory
{
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Parameter metadata creation uses reflection")]
#endif
    public static ParameterMetadata[] Build(
        ParameterInfo[] parameters,
        string? nameFallback,
        bool computeIsNullable)
    {
        if (parameters.Length == 0)
        {
            return [];
        }

        var result = new ParameterMetadata[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            result[i] = Create(p.ParameterType, p.Name ?? nameFallback, i, p, computeIsNullable);
        }

        return result;
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Parameter metadata creation uses reflection")]
#endif
    private static ParameterMetadata Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.PublicProperties)] Type parameterType,
        string? name,
        int index,
        ParameterInfo reflectionInfo,
        bool computeIsNullable)
    {
        return new ParameterMetadata(parameterType)
        {
            Name = name ?? $"param{index}",
            TypeInfo = new ConcreteType(parameterType),
            ReflectionInfo = reflectionInfo,
            Type = parameterType,
            IsNullable = computeIsNullable
                && (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>)
                    || Nullable.GetUnderlyingType(parameterType) != null)
        };
    }
}
