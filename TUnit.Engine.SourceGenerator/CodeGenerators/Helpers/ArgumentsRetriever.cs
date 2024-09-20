﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class ArgumentsRetriever
{
    public static IEnumerable<ArgumentsContainer> GetArguments(GeneratorAttributeSyntaxContext context,
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<AttributeData> dataAttributes,
        INamedTypeSymbol namedTypeSymbol,
        string argPrefix)
    {
        if (parameters.IsDefaultOrEmpty || !IsDataDriven(dataAttributes, parameters))
        {
            yield return new ArgumentsContainer
            {
                Arguments = [],
                DataAttribute = null,
                DataAttributeIndex = null,
                IsEnumerableData = false
            };
            
            yield break;
        }

        foreach (var argumentsContainer in MatrixRetriever.Parse(parameters))
        {
            yield return argumentsContainer;
        }

        for (var index = 0; index < dataAttributes.Length; index++)
        {
            var dataAttribute = dataAttributes.ElementAtOrDefault(index);
            
            if (dataAttribute is null)
            {
                continue;
            }
            
            var name = dataAttribute.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
            
            if (name == WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithGlobalPrefix)
            {
                yield return DataDrivenArgumentsRetriever.ParseArguments(dataAttribute, parameters, index);
            }
            
            if (name == WellKnownFullyQualifiedClassNames.MethodDataSourceAttribute.WithGlobalPrefix)
            {
                yield return MethodDataSourceRetriever.ParseMethodData(context, parameters, namedTypeSymbol, dataAttribute, argPrefix, index);
            }
            
            if (name == WellKnownFullyQualifiedClassNames.ClassDataSourceAttribute.WithGlobalPrefix)
            {
                yield return ClassDataSourceRetriever.ParseClassData(namedTypeSymbol, dataAttribute, index);
            }
            
            if (name == WellKnownFullyQualifiedClassNames.ClassConstructorAttribute.WithGlobalPrefix)
            {
                yield return ClassConstructorRetriever.Parse(namedTypeSymbol, dataAttribute, index);
            }
        }
    }

    private static bool IsDataDriven(ImmutableArray<AttributeData> dataAttributes,
        ImmutableArray<IParameterSymbol> parameters)
    {
        return dataAttributes.Any(x => x.IsDataSourceAttribute())
               || parameters.HasMatrixAttribute();
    }
}