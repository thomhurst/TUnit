using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class CombinativeValuesRetriever
{
    // We return a List of a List. Inner List is for each test.
    public static IEnumerable<ArgumentsContainer> Parse(IMethodSymbol methodSymbol, AttributeData[] methodAndClassAttributes)
    {
        var methodSymbolParameters = methodSymbol.Parameters;
        
        if (methodSymbolParameters.IsDefaultOrEmpty)
        {
            return [];
        }
        
        var combinativeValuesAttributes = methodSymbolParameters
            .Select(x => x.GetAttributes().SafeFirstOrDefault(x => x.GetFullyQualifiedAttributeTypeName()
                                                               == WellKnownFullyQualifiedClassNames.CombinativeValuesAttribute.WithGlobalPrefix))
            .OfType<AttributeData>()
            .ToList();
        
        var mappedToConstructorArrays = combinativeValuesAttributes
            .Select(x => x.ConstructorArguments.SafeFirstOrDefault().Values);

        var attr = combinativeValuesAttributes.SafeFirstOrDefault();
        
        var index = 0;
        return GetCombinativeArgumentsList(mappedToConstructorArrays)
            .Select(x => MapToArgumentEnumerable(x, methodSymbolParameters))
            .Select(x => new ArgumentsContainer
            {
                DataAttribute = attr,
                DataAttributeIndex = ++index,
                IsEnumerableData = false,
                Arguments = [..x]
            });
    }

    private static IEnumerable<Argument> MapToArgumentEnumerable(IEnumerable<TypedConstant> typedConstants, ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        return typedConstants.Select((typedConstant, index) =>
            {
                var parameterSymbolType = parameterSymbols.ElementAt(index).Type;
                
                return new Argument(ArgumentSource.CombinativeDataAttribute,
                    parameterSymbolType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                    TypedConstantParser.GetTypedConstantValue(typedConstant, parameterSymbolType));
            });
    }

    private static readonly IEnumerable<IEnumerable<TypedConstant>> Seed = [[]];
    
    private static IEnumerable<IEnumerable<TypedConstant>> GetCombinativeArgumentsList(IEnumerable<ImmutableArray<TypedConstant>> elements)
    {
        return elements.Aggregate(Seed, (accumulator, enumerable)
            => accumulator.SelectMany(x => enumerable.Select(x.Append)));
    }
}