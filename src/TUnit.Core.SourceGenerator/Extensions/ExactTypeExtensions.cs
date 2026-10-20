using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

/// <summary>
/// Shared by TUnit.Core.SourceGenerator and TUnit.Analyzers (linked into the analyzer project) so that the
/// generator's run-time explanation and analyzer TUnit0001 classify declared data source types the same way (#7116).
/// </summary>
internal static class ExactTypeExtensions
{
    /// <summary>
    /// Whether every value of <paramref name="type"/> has exactly that run-time type, so a constraint it breaks
    /// cannot be met by a value of the data source. <c>object</c>, interfaces, non-sealed classes, nullable value
    /// types, covariant arrays and types that contain a type parameter are not exact.
    /// </summary>
    public static bool IsExactType(this ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol or IErrorTypeSymbol || type.TypeKind == TypeKind.Dynamic
            || ContainsTypeParameter(type))
        {
            return false;
        }

        if (type.IsValueType)
        {
            return type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;
        }

        // Arrays of reference types are covariant: an object[] value may be a string[]. An array of a value
        // type or of an exact type cannot hold another array type.
        if (type is IArrayTypeSymbol array)
        {
            return array.ElementType.IsValueType || IsExactType(array.ElementType);
        }

        return type is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Delegate, IsSealed: true };
    }

    private static bool ContainsTypeParameter(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => true,
        IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
        IPointerTypeSymbol pointer => ContainsTypeParameter(pointer.PointedAtType),
        INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter)
            || (named.ContainingType is { } containingType && ContainsTypeParameter(containingType)),
        _ => false
    };
}
