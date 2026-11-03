using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

/// <summary>
/// Computes validation targets for a row after generic inference, following invocation binding.
/// The declared signature selects injected tokens; the closed signature supplies the expected types.
/// </summary>
internal static class TypedRowParameterBinding
{
    public static ImmutableArray<ITypeSymbol> GetExpectedTypes(Compilation compilation,
        IMethodSymbol method, IMethodSymbol constructedMethod, ImmutableArray<ITypeSymbol> argumentTypes)
    {
        // Injection follows the declared signature: a T inferred as CancellationToken still takes data.
        var closedParameters = method.Parameters.WithoutTestMethodCancellationTokens()
            .Select(p => constructedMethod.Parameters[p.Ordinal]).ToArray();
        // Preserve the declared trailing position before dropping omitted optional parameters.
        var trailingParameter = closedParameters.LastOrDefault();
        // A shorter row leaves optional parameters at their defaults and a trailing SZ array empty.
        var suppliedParameterCount = argumentTypes.Length;
        if (suppliedParameterCount < closedParameters.Length
            && closedParameters.Skip(suppliedParameterCount).All(p => p.IsOptional || p.HasExplicitDefaultValue || p.IsParams
                || (SymbolEqualityComparer.Default.Equals(p, trailingParameter)
                    && p.Type is IArrayTypeSymbol { IsSZArray: true })))
        {
            closedParameters = closedParameters.Take(suppliedParameterCount).ToArray();
        }

        var expectedTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();
        for (var i = 0; i < closedParameters.Length; i++)
        {
            var parameter = closedParameters[i];
            if (SymbolEqualityComparer.Default.Equals(parameter, trailingParameter)
                && (parameter.IsParams || parameter.Type is IArrayTypeSymbol { IsSZArray: true })
                && GetParamsElementType(parameter.Type) is { } elementType && i < argumentTypes.Length
                && (argumentTypes.Length > closedParameters.Length
                    || !compilation.HasImplicitConversionOrGenericParameter(argumentTypes[i], parameter.Type)))
            {
                // A single supplied array is converted element-wise by ToTrailingArray unless
                // the whole array itself binds as one element (for example params object[]).
                if (argumentTypes.Length == closedParameters.Length
                    && argumentTypes[i] is IArrayTypeSymbol { Rank: 1 } suppliedArray
                    && !compilation.HasImplicitConversionOrGenericParameter(suppliedArray, elementType)
                    && compilation.HasImplicitConversionOrGenericParameter(suppliedArray.ElementType, elementType))
                {
                    expectedTypes.Add(suppliedArray);
                    continue;
                }

                // Every loose value binds to the original trailing parameter's element type.
                for (var argumentIndex = i; argumentIndex < argumentTypes.Length; argumentIndex++)
                {
                    expectedTypes.Add(elementType);
                }
            }
            else
            {
                expectedTypes.Add(parameter.Type);
            }
        }

        return expectedTypes.ToImmutable();
    }

    // Match the array-assignable params collections supported by TupleArgumentHelper in discovery.
    private static ITypeSymbol? GetParamsElementType(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol { IsSZArray: true } array => array.ElementType,
        INamedTypeSymbol { IsGenericType: true } named when named.OriginalDefinition.SpecialType is
            SpecialType.System_Collections_Generic_IEnumerable_T
            or SpecialType.System_Collections_Generic_ICollection_T
            or SpecialType.System_Collections_Generic_IList_T
            or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
            or SpecialType.System_Collections_Generic_IReadOnlyList_T => named.TypeArguments[0],
        _ => null
    };
}
