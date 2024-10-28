using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class SymbolExtensions
{
    public static bool HasDataDrivenAttributes(this ISymbol symbol)
    {
        var attributes = symbol.GetAttributes();

        return attributes.Any(a => a.AttributeClass?.AllInterfaces.Any(x =>
            x.GloballyQualified() == WellKnown.AttributeFullyQualifiedClasses.IDataAttribute.WithGlobalPrefix) == true)
               || HasMatrixValues(symbol);
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
            WellKnown.AttributeFullyQualifiedClasses.Matrix.WithGlobalPrefix));
    }
}