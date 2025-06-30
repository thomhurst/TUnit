using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class ParameterExtensions
{
    public static ImmutableArray<IParameterSymbol> WithoutCancellationTokenParameter(this ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        if (parameterSymbols.IsDefaultOrEmpty)
        {
            return parameterSymbols;
        }

        if (parameterSymbols.Last().Type.GloballyQualified() ==
            WellKnownFullyQualifiedClassNames.CancellationToken.WithGlobalPrefix)
        {
            return ImmutableArray.Create(parameterSymbols, 0, parameterSymbols.Length - 1);
        }

        return parameterSymbols;
    }
}
