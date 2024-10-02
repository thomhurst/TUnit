using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class ArgumentsRetriever
{
    public static IEnumerable<ArgumentsContainer> GetArguments(GeneratorAttributeSyntaxContext context,
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<AttributeData> dataAttributes,
        INamedTypeSymbol namedTypeSymbol,
        ArgumentsType argumentsType)
    {
        if (parameters.IsDefaultOrEmpty || !IsDataDriven(dataAttributes, parameters))
        {
            yield return new EmptyArgumentsContainer
            {
                ArgumentsType = argumentsType,
                DisposeAfterTest = false
            };
            
            yield break;
        }

        foreach (var argumentsContainer in MatrixRetriever.Parse(context, parameters, argumentsType))
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
                yield return DataDrivenArgumentsRetriever.ParseArguments(context, dataAttribute, parameters, argumentsType, index);
            }
            
            if (name == WellKnownFullyQualifiedClassNames.MethodDataSourceAttribute.WithGlobalPrefix)
            {
                yield return MethodDataSourceRetriever.ParseMethodData(context, parameters, namedTypeSymbol, dataAttribute, argumentsType, index);
            }
            
            if (name == WellKnownFullyQualifiedClassNames.ClassDataSourceAttribute.WithGlobalPrefix)
            {
                yield return ClassDataSourceRetriever.ParseClassData(namedTypeSymbol, dataAttribute, argumentsType, index);
            }
            
            if (name == WellKnownFullyQualifiedClassNames.ClassConstructorAttribute.WithGlobalPrefix)
            {
                yield return ClassConstructorRetriever.Parse(dataAttribute, index);
            }
            
            if (dataAttribute.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames.DataSourceGeneratorAttribute.WithGlobalPrefix) == true)
            {
                yield return DataSourceGeneratorRetriever.Parse(parameters, namedTypeSymbol, dataAttribute, argumentsType, index);
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