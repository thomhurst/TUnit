using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class DataDrivenArgumentsRetriever
{
    public static ArgumentsContainer ParseArguments(AttributeData argumentAttribute, ImmutableArray<IParameterSymbol> parameterSymbols,
        int dataAttributeIndex)
    {
        var constructorArgument = argumentAttribute.ConstructorArguments.SafeFirstOrDefault();

        if (constructorArgument.IsNull)
        {
            return new ArgumentsContainer
            {
                DataAttribute = argumentAttribute,
                DataAttributeIndex = dataAttributeIndex,
                IsEnumerableData = false,
                Arguments =
                [
                    new Argument(type: parameterSymbols.SafeFirstOrDefault()?.Type
                            .ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ?? "var",
                        invocation: null
                    ),
                ]
            };
        }
        
        var objectArray = constructorArgument.Values;

        var args = GetArguments(objectArray, parameterSymbols);

        return new ArgumentsContainer
        {
            DataAttribute = argumentAttribute,
            DataAttributeIndex = dataAttributeIndex,
            IsEnumerableData = false,
            Arguments = [..args]
        };
    }

    private static IEnumerable<Argument> GetArguments(ImmutableArray<TypedConstant> objectArray,
        ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        if (objectArray.IsDefaultOrEmpty)
        {
            var type = parameterSymbols.SafeFirstOrDefault()?.Type
                ?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ?? "var";
            
            return [new Argument(type, null)];
        }

        return objectArray.Select((x, i) =>
        {
            var type = GetTypeFromParameters(parameterSymbols, i);
            
            return new Argument(type?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ??
                                TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(x),
                TypedConstantParser.GetTypedConstantValue(x, type));
        });
    }

    private static ITypeSymbol? GetTypeFromParameters(ImmutableArray<IParameterSymbol> parameterSymbols, int index)
    {
        if (parameterSymbols.IsDefaultOrEmpty)
        {
            return null;
        }

        return parameterSymbols.ElementAtOrDefault(index)?.Type;
    }
}