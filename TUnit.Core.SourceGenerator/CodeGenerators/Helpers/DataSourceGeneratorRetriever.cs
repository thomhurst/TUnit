using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class DataSourceGeneratorRetriever
{
    public static ArgumentsContainer Parse(GeneratorAttributeSyntaxContext context,
        INamedTypeSymbol testClass,
        IMethodSymbol testMethod,
        ImmutableArray<IParameterSymbol> parameters,
        IPropertySymbol? property,
        ImmutableArray<ITypeSymbol> parameterOrPropertyTypes,
        AttributeData attributeData,
        ArgumentsType argumentsType,
        int index,
        string? propertyName,
        bool isStronglyTyped)
    {
        return new AsyncDataSourceGeneratorContainer
        (
            Context: context,
            AttributeData: attributeData,
            ArgumentsType: argumentsType,
            ParameterOrPropertyTypes: parameterOrPropertyTypes,
            TestClass: testClass,
            TestMethod: testMethod,
            Property: property,
            Parameters: parameters,
            GenericArguments: GetDataGeneratorAttributeBaseClass(attributeData.AttributeClass)?.TypeArguments.Select(x => x.GloballyQualified()).ToArray() ?? []
        )
        {
            DisposeAfterTest =
                attributeData.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest")
                    .Value.Value as bool? ??
                true,
            PropertyName = propertyName,
            Attribute = attributeData,
            AttributeIndex = index,
            IsStronglyTyped = isStronglyTyped,
        };
    }

    private static INamedTypeSymbol? GetDataGeneratorAttributeBaseClass(INamedTypeSymbol? attributeClass)
    {
        var selfAndBaseTypes = attributeClass?.GetSelfAndBaseTypes();

        if (selfAndBaseTypes?.FirstOrDefault(HasGeneratorInterface) is INamedTypeSymbol generatorInterface)
        {
            return generatorInterface;
        }

        return null;
    }

    private static bool HasGeneratorInterface(ITypeSymbol t)
    {
        var interfaces = t.Interfaces.Select(i => i.GloballyQualified()).ToList();
        return interfaces.Contains(WellKnownFullyQualifiedClassNames.IAsyncDataSourceGeneratorAttribute.WithGlobalPrefix);
    }
}
