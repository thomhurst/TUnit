using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class DataDrivenArgumentsRetriever
{
    public static IEnumerable<ArgumentsContainer> Parse(ImmutableArray<AttributeData> methodAttributes,
        AttributeData[] testAndClassAttributes, ImmutableArray<IParameterSymbol> methodSymbolParameters)
    {
        var index = 0;
        return methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                 == WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithGlobalPrefix)
            .Select(argumentAttribute => ParseArguments(testAndClassAttributes, argumentAttribute, methodSymbolParameters, ++index));
    }

    private static ArgumentsContainer ParseArguments(AttributeData[] testAndClassAttributes,
        AttributeData argumentAttribute, ImmutableArray<IParameterSymbol> parameterSymbols,
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
                    new Argument(
                        argumentSource: ArgumentSource.ArgumentAttribute,
                        type: parameterSymbols.SafeFirstOrDefault()?.Type
                            .ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ?? "var",
                        invocation: null
                    ),
                    ..Array.Empty<Argument>().WithTimeoutArgument(testAndClassAttributes)
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
            Arguments = [..args.WithTimeoutArgument(testAndClassAttributes)]
        };
    }

    private static IEnumerable<Argument> GetArguments(ImmutableArray<TypedConstant> objectArray,
        ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        if (objectArray.IsDefaultOrEmpty)
        {
            var type = parameterSymbols.SafeFirstOrDefault()?.Type
                ?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ?? "var";
            
            return [new Argument(ArgumentSource.ArgumentAttribute, type, null)];
        }

        return objectArray.Select((x, i) =>
        {
            var type = GetTypeFromParameters(parameterSymbols, i);
            
            return new Argument(ArgumentSource.ArgumentAttribute,
                type?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ??
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