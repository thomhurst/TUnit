using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class ParameterExtensions
{
    /// <summary>
    /// Removes CancellationToken parameters, which take no data: the engine passes the test's token
    /// to them wherever they are declared.
    /// </summary>
    public static ImmutableArray<IParameterSymbol> WithoutCancellationTokenParameter(this ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        if (parameterSymbols.IsDefaultOrEmpty
            || !parameterSymbols.Any(static p => IsCancellationToken(p.Type)))
        {
            return parameterSymbols;
        }

        return parameterSymbols.RemoveAll(static p => IsCancellationToken(p.Type));
    }

    /// <inheritdoc cref="WithoutCancellationTokenParameter(ImmutableArray{IParameterSymbol})"/>
    public static ImmutableArray<ITypeSymbol> WithoutCancellationTokenParameter(this ImmutableArray<ITypeSymbol> typeSymbols)
    {
        if (typeSymbols.IsDefaultOrEmpty
            || !typeSymbols.Any(static t => IsCancellationToken(t)))
        {
            return typeSymbols;
        }

        return typeSymbols.RemoveAll(static t => IsCancellationToken(t));
    }

    private static bool IsCancellationToken(ITypeSymbol type)
        => type.IsGloballyQualifiedNonGeneric(WellKnown.AttributeFullyQualifiedClasses.CancellationToken.WithGlobalPrefix);

    public static bool HasMatrixAttribute(this IParameterSymbol parameterSymbol, Compilation compilation)
    {
        return parameterSymbol.GetAttributes().Any(x => x.IsMatrixAttribute(compilation));
    }

    public static bool HasDataSourceAttribute(this IParameterSymbol parameterSymbol, Compilation compilation)
    {
        return parameterSymbol.GetAttributes().Any(x => x.IsDataSourceAttribute(compilation));
    }
}
