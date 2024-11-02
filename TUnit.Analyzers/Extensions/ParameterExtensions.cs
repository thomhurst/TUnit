using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class ParameterExtensions
{
    public static IEnumerable<IParameterSymbol> WithoutCancellationTokenParameter(this ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        if (parameterSymbols.IsDefaultOrEmpty)
        {
            return parameterSymbols;
        }

        if (parameterSymbols.Last().Type.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) ==
            WellKnown.AttributeFullyQualifiedClasses.CancellationToken.WithGlobalPrefix)
        {
            return parameterSymbols.Take(parameterSymbols.Length - 1);
        }

        return parameterSymbols;
    }
    
    public static IEnumerable<ITypeSymbol> WithoutCancellationTokenParameter(this ImmutableArray<ITypeSymbol> typeSymbols)
    {
        if (typeSymbols.IsDefaultOrEmpty)
        {
            return typeSymbols;
        }

        if (typeSymbols.Last().ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) ==
            WellKnown.AttributeFullyQualifiedClasses.CancellationToken.WithGlobalPrefix)
        {
            return typeSymbols.Take(typeSymbols.Length - 1);
        }

        return typeSymbols;
    }
    
    public static bool HasMatrixAttribute(this IParameterSymbol parameterSymbol, Compilation compilation)
    {
        return parameterSymbol.GetAttributes().Any(x => x.IsMatrixAttribute(compilation));
    }
}