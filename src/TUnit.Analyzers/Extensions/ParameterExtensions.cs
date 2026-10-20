using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class ParameterExtensions
{
    /// <summary>
    /// Removes the CancellationToken parameters of a test method, which take no data: the engine passes
    /// the test's token to them wherever they are declared.
    /// </summary>
    /// <remarks>Keep in step with the other copies of this rule: TUnit0001 (<c>ParameterExtensions.WithoutTestMethodCancellationTokens</c>), the generator (<c>TestMetadataGenerator.GetDataParameters</c>), the runtime (<c>DataGeneratorMetadataCreator.FilterOutCancellationToken</c>, <c>DataUnwrapper</c>) and reflection discovery (<c>ReflectionTestMetadata</c>, <c>ReflectionTestDataCollector</c>).</remarks>
    public static ImmutableArray<IParameterSymbol> WithoutTestMethodCancellationTokens(this ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        if (parameterSymbols.IsDefaultOrEmpty
            || !parameterSymbols.Any(static p => IsCancellationToken(p.Type)))
        {
            return parameterSymbols;
        }

        return parameterSymbols.RemoveAll(static p => IsCancellationToken(p.Type));
    }

    /// <summary>
    /// Removes a trailing CancellationToken parameter. Constructors and data source methods only skip a
    /// trailing token; one declared elsewhere takes data. Use <see cref="WithoutTestMethodCancellationTokens"/>
    /// for test methods.
    /// </summary>
    public static ImmutableArray<IParameterSymbol> WithoutCancellationTokenParameter(this ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        if (parameterSymbols.IsDefaultOrEmpty || !IsCancellationToken(parameterSymbols[parameterSymbols.Length - 1].Type))
        {
            return parameterSymbols;
        }

        return ImmutableArray.Create(parameterSymbols, 0, parameterSymbols.Length - 1);
    }

    /// <inheritdoc cref="WithoutCancellationTokenParameter(ImmutableArray{IParameterSymbol})"/>
    public static ImmutableArray<ITypeSymbol> WithoutCancellationTokenParameter(this ImmutableArray<ITypeSymbol> typeSymbols)
    {
        if (typeSymbols.IsDefaultOrEmpty || !IsCancellationToken(typeSymbols[typeSymbols.Length - 1]))
        {
            return typeSymbols;
        }

        return ImmutableArray.Create(typeSymbols, 0, typeSymbols.Length - 1);
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
