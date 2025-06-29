using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class ParameterExtensions
{
    public static ImmutableArray<IParameterSymbol> WithoutCancellationTokenParameter(this ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        if (parameterSymbols.IsDefaultOrEmpty)
        {
            return parameterSymbols;
        }

        if (parameterSymbols.Last().Type.GloballyQualifiedNonGeneric() ==
            WellKnown.AttributeFullyQualifiedClasses.CancellationToken.WithGlobalPrefix)
        {
            return ImmutableArray.Create(parameterSymbols, 0, parameterSymbols.Length - 1);
        }

        return parameterSymbols;
    }

    public static ImmutableArray<ITypeSymbol> WithoutCancellationTokenParameter(this ImmutableArray<ITypeSymbol> typeSymbols)
    {
        if (typeSymbols.IsDefaultOrEmpty)
        {
            return typeSymbols;
        }

        if (typeSymbols.Last().GloballyQualified() ==
            WellKnown.AttributeFullyQualifiedClasses.CancellationToken.WithGlobalPrefix)
        {
            return ImmutableArray.Create(typeSymbols, 0, typeSymbols.Length - 1);
        }

        return typeSymbols;
    }

    public static bool HasMatrixAttribute(this IParameterSymbol parameterSymbol, Compilation compilation)
    {
        return parameterSymbol.GetAttributes().Any(x => x.IsMatrixAttribute(compilation));
    }
}
