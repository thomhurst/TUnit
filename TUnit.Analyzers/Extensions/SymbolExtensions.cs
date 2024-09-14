using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

internal static class SymbolExtensions
{
    private static readonly string[] DataDrivenAttributes =
    [
        WellKnown.AttributeFullyQualifiedClasses.ClassDataSource,
        WellKnown.AttributeFullyQualifiedClasses.MethodDataSource,
        WellKnown.AttributeFullyQualifiedClasses.EnumerableMethodDataSource,
        WellKnown.AttributeFullyQualifiedClasses.Arguments,
        WellKnown.AttributeFullyQualifiedClasses.ClassConstructor
    ];
    
    public static bool HasDataDrivenAttributes(this ISymbol symbol)
    {
        var attributes = symbol.GetAttributes();

        var hasDataDrivenAttributes = attributes.Select(x =>
                x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            )
            .Intersect(DataDrivenAttributes)
            .Any();

        return hasDataDrivenAttributes || HasMatrixValues(symbol);
    }

    private static bool HasMatrixValues(ISymbol symbol)
    {
        var parameters = symbol switch
        {
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters,
            IMethodSymbol methodSymbol => methodSymbol.Parameters,
            _ => null
        };

        if (parameters == null || parameters.Value.IsDefaultOrEmpty)
        {
            return false;
        }

        return parameters.Value.Any(p => p.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) ==
            WellKnown.AttributeFullyQualifiedClasses.Matrix));
    }
}