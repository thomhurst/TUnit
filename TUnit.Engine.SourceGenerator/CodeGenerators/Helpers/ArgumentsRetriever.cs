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
        ImmutableArray<ITypeSymbol> parameterOrPropertyTypes,
        ImmutableArray<AttributeData> dataAttributes,
        INamedTypeSymbol namedTypeSymbol,
        ArgumentsType argumentsType)
    {
        if (parameters.IsDefaultOrEmpty || !IsDataDriven(dataAttributes, parameters))
        {
            yield return new EmptyArgumentsContainer(argumentsType)
            {
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
                yield return DataDrivenArgumentsRetriever.ParseArguments(context, dataAttribute, parameterOrPropertyTypes, argumentsType, index);
            }
            
            if (name == WellKnownFullyQualifiedClassNames.MethodDataSourceAttribute.WithGlobalPrefix)
            {
                yield return MethodDataSourceRetriever.ParseMethodData(context, parameterOrPropertyTypes, namedTypeSymbol, dataAttribute, argumentsType, index);
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

    public static ClassPropertiesContainer GetProperties(GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol)
    {
        var list = new List<(IPropertySymbol, ArgumentsContainer)>();

        foreach (var propertySymbol in namedTypeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var dataSourceAttributes = propertySymbol.GetAttributes().Where(x => x.IsDataSourceAttribute()).ToImmutableArray();
            if (dataSourceAttributes.Any())
            {
                list.AddRange(GetArguments(context, ImmutableArray<IParameterSymbol>.Empty, ImmutableArray.Create(propertySymbol.Type), dataSourceAttributes, namedTypeSymbol, ArgumentsType.Property).Select(argumentsContainer => (propertySymbol, argumentsContainer)));
            }
        }

        return new ClassPropertiesContainer(list);
    }
}