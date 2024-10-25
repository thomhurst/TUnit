using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

internal static class SymbolExtensions
{
    public static bool HasDataSourceAttribute(this ISymbol symbol)
    {
        return symbol.GetAttributes().Any(x => x.IsDataSourceAttribute());
    }
}