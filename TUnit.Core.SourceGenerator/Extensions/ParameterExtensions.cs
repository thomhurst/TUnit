using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class ParameterExtensions
{
    public static bool HasMatrixAttribute(this ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        return parameterSymbols.Any(p =>
            p.GetAttributes().Any(a =>
                a.IsMatrixAttribute()
            )
        );
    }
}