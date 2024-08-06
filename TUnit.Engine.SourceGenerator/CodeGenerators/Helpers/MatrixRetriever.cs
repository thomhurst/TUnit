using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class MatrixRetriever
{
    // We return a List of a List. Inner List is for each test.
    public static IEnumerable<ArgumentsContainer> Parse(ImmutableArray<IParameterSymbol> parameters)
    {
        if (parameters.IsDefaultOrEmpty || !parameters.HasMatrixAttribute())
        {
            return [];
        }
        
        var matrixAttributes = parameters
            .Select(p => p.GetAttributes().SafeFirstOrDefault(a => a.GetFullyQualifiedAttributeTypeName()
                                                               == WellKnownFullyQualifiedClassNames.MatrixAttribute.WithGlobalPrefix))
            .OfType<AttributeData>()
            .ToList();
        
        var mappedToConstructorArrays = matrixAttributes
            .Select(x => x.ConstructorArguments.SafeFirstOrDefault().Values);

        var attr = matrixAttributes.SafeFirstOrDefault();
        
        var index = 0;
        return GetMatrixArgumentsList(mappedToConstructorArrays)
            .Select(x => MapToArgumentEnumerable(x, parameters))
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
                
                return new Argument(parameterSymbolType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                    TypedConstantParser.GetTypedConstantValue(typedConstant, parameterSymbolType));
            });
    }

    private static readonly IEnumerable<IEnumerable<TypedConstant>> Seed = [[]];
    
    private static IEnumerable<IEnumerable<TypedConstant>> GetMatrixArgumentsList(IEnumerable<ImmutableArray<TypedConstant>> elements)
    {
        return elements.Aggregate(Seed, (accumulator, enumerable)
            => accumulator.SelectMany(x => enumerable.Select(x.Append)));
    }
}