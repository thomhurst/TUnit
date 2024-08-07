using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class ParameterExtensions
{
    public static IEnumerable<IParameterSymbol> WithoutTimeoutParameter(this ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        if (parameterSymbols.IsDefaultOrEmpty)
        {
            return parameterSymbols;
        }

        if (parameterSymbols.Last().Type.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) ==
            WellKnown.AttributeFullyQualifiedClasses.CancellationToken)
        {
            return parameterSymbols.Take(parameterSymbols.Length - 1);
        }

        return parameterSymbols;
    }
}