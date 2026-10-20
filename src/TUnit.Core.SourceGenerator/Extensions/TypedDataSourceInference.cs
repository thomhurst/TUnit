using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

/// <summary>
/// Shared by TUnit.Core.SourceGenerator and TUnit.Analyzers (linked into the analyzer project) so that the generator
/// and analyzer TUnit0001 infer a generic test method's type arguments from a typed data source the same way (#7116).
/// Reflection mode's <c>InferTypesFromTypedDataSource</c> applies the same rules to run-time types.
/// </summary>
internal static class TypedDataSourceInference
{
    /// <summary>
    /// Infers the type arguments of a generic test method on a non-generic class from a typed data source, whose
    /// type arguments come from its <c>DataSourceGeneratorAttribute</c> / <c>AsyncDataSourceGeneratorAttribute</c>
    /// base. One type parameter takes the first type argument; otherwise a single tuple argument with one element per
    /// type parameter, or one type argument per type parameter. Returns <see langword="null"/> when nothing is inferred.
    /// </summary>
    public static ITypeSymbol[]? InferMethodTypeArguments(INamedTypeSymbol? attributeClass, IMethodSymbol method)
    {
        if (!method.IsGenericMethod || method.ContainingType.IsGenericType)
        {
            return null;
        }

        var typeParameterCount = method.TypeParameters.Length;
        for (var baseType = attributeClass?.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (!IsTypedDataSourceBase(baseType))
            {
                continue;
            }

            var typeArguments = baseType.TypeArguments;
            if (typeParameterCount == 1 && typeArguments.Length >= 1)
            {
                return [typeArguments[0]];
            }

            if (typeArguments is [INamedTypeSymbol { IsTupleType: true } tupleType]
                && tupleType.TupleElements.Length == typeParameterCount)
            {
                return tupleType.TupleElements.Select(e => e.Type).ToArray();
            }

            if (typeArguments.Length == typeParameterCount)
            {
                return typeArguments.ToArray();
            }
        }

        return null;
    }

    /// <summary>
    /// Whether <paramref name="baseType"/> is a constructed <c>DataSourceGeneratorAttribute</c> or
    /// <c>AsyncDataSourceGeneratorAttribute</c> from TUnit.Core.
    /// </summary>
    public static bool IsTypedDataSourceBase(INamedTypeSymbol baseType)
        => baseType.IsGenericType
            && baseType.OriginalDefinition.Name is "DataSourceGeneratorAttribute" or "AsyncDataSourceGeneratorAttribute"
            && baseType.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "TUnit.Core";
}
