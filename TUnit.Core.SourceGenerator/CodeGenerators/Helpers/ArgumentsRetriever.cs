using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class ArgumentsRetriever
{
    public static IEnumerable<BaseContainer> GetArguments(GeneratorAttributeSyntaxContext context,
        ImmutableArray<IParameterSymbol> parameters,
        IPropertySymbol? property,
        ImmutableArray<ITypeSymbol> parameterOrPropertyTypes,
        ImmutableArray<AttributeData> dataAttributes,
        INamedTypeSymbol testClass,
        IMethodSymbol testMethod,
        ArgumentsType argumentsType,
        string? propertyName = null)
    {
        if (parameterOrPropertyTypes.IsDefaultOrEmpty || !IsDataDriven(dataAttributes))
        {
            yield return new EmptyArgumentsContainer();

            yield break;
        }

        foreach (var attributeTypeGroup in dataAttributes.GroupBy(x => x.AttributeClass,
                     SymbolEqualityComparer.Default))
        {
            for (var index = 0; index < attributeTypeGroup.Count(); index++)
            {
                var dataAttribute = attributeTypeGroup.ElementAtOrDefault(index);

                if (dataAttribute is null)
                {
                    continue;
                }

                var name = dataAttribute.AttributeClass?.ToDisplayString(DisplayFormats
                    .FullyQualifiedNonGenericWithGlobalPrefix);

                if (name == WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithGlobalPrefix)
                {
                    yield return DataDrivenArgumentsRetriever.ParseArguments(context, dataAttribute,
                        parameterOrPropertyTypes, argumentsType, index);
                }

                else if (name == WellKnownFullyQualifiedClassNames.MethodDataSourceAttribute.WithGlobalPrefix
                         || name == WellKnownFullyQualifiedClassNames.InstanceMethodDataSourceAttribute.WithGlobalPrefix)
                {
                    yield return MethodDataSourceRetriever.ParseMethodData(context, parameterOrPropertyTypes,
                        testClass, dataAttribute, argumentsType, index);
                }

                else if (name == WellKnownFullyQualifiedClassNames.ClassConstructorAttribute.WithGlobalPrefix)
                {
                    yield return ClassConstructorRetriever.Parse(dataAttribute, index);
                }

                else if (dataAttribute.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames
                             .DataSourceGeneratorAttribute.WithGlobalPrefix) == true)
                {
                    yield return DataSourceGeneratorRetriever.Parse(context,
                        testClass,
                        testMethod,
                        parameters,
                        property,
                        parameterOrPropertyTypes,
                        dataAttribute,
                        argumentsType,
                        index,
                        propertyName,
                        true);
                }

                else if (dataAttribute.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames
                        .AsyncDataSourceGeneratorAttribute.WithGlobalPrefix) == true)
                {
                    yield return AsyncDataSourceGeneratorRetriever.Parse(context,
                        testClass,
                        testMethod,
                        parameters,
                        property,
                        parameterOrPropertyTypes,
                        dataAttribute,
                        argumentsType,
                        index,
                        propertyName,
                        true);
                }

                else if (dataAttribute.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames
                             .AsyncUntypedDataSourceGeneratorAttribute.WithGlobalPrefix) == true)
                {
                    yield return AsyncDataSourceGeneratorRetriever.Parse(context,
                        testClass,
                        testMethod,
                        parameters,
                        property,
                        parameterOrPropertyTypes,
                        dataAttribute,
                        argumentsType,
                        index,
                        propertyName,
                        false);
                }
            }
        }
    }

    private static bool IsDataDriven(ImmutableArray<AttributeData> dataAttributes)
    {
        return dataAttributes.Any(x => x.IsDataSourceAttribute());
    }

    public static ClassPropertiesContainer GetProperties(GeneratorAttributeSyntaxContext context,
        INamedTypeSymbol namedTypeSymbol, IMethodSymbol methodSymbol)
    {
        var settableProperties = namedTypeSymbol
            .GetSelfAndBaseTypes()
            .SelectMany(x => x.GetMembers())
            .OfType<IPropertySymbol>()
            .Where(x => x.IsRequired || x.IsStatic)
            .ToList();

        if (!settableProperties.Any())
        {
            return new ClassPropertiesContainer([])
            {
                DisposeAfterTest = false,
                AttributeIndex = 0,
                Attribute = context.Attributes.FirstOrDefault()!,
            };
        }

        var list = new List<(IPropertySymbol, ArgumentsContainer)>();

        foreach (var propertySymbol in settableProperties)
        {
            var dataSourceAttributes = propertySymbol.GetAttributes().Where(x => x.IsDataSourceAttribute()).ToImmutableArray();
            if (dataSourceAttributes.Any())
            {
                var args = GetArguments(context,
                    ImmutableArray<IParameterSymbol>.Empty,
                    propertySymbol,
                    [propertySymbol.Type],
                    dataSourceAttributes,
                    namedTypeSymbol,
                    methodSymbol,
                    ArgumentsType.Property, propertySymbol.Name);

                list.Add((propertySymbol, args.OfType<ArgumentsContainer>().First()));
            }
        }

        return new ClassPropertiesContainer(list)
        {
            DisposeAfterTest = false,
            Attribute = null!,
            AttributeIndex = 0
        };
    }
}
