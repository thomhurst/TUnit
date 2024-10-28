using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class DataSourceGeneratorRetriever
{
    public static ArgumentsContainer Parse(
        INamedTypeSymbol namedTypeSymbol, 
        AttributeData attributeData, 
        ArgumentsType argumentsType, 
        int index,
        string? propertyName)
    {
        return new GeneratedArgumentsContainer
        (
            ArgumentsType: argumentsType,
            TestClassTypeName: namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            AttributeDataGeneratorType: attributeData.AttributeClass!.ToDisplayString(DisplayFormats
                .FullyQualifiedGenericWithGlobalPrefix),
            GenericArguments: GetDataGeneratorAttributeBaseClass(attributeData.AttributeClass).TypeArguments
                .Select(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)).ToArray(),
            AttributeIndex: index
        )
        {
            DisposeAfterTest =
                attributeData.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest")
                    .Value.Value as bool? ??
                true,
            PropertyName = propertyName,
            Attribute = attributeData,
            AttributeIndex = index
        };
    }

    private static INamedTypeSymbol GetDataGeneratorAttributeBaseClass(ITypeSymbol attributeClass)
    {
        var selfAndBaseTypes = attributeClass.GetSelfAndBaseTypes();

        return (INamedTypeSymbol) selfAndBaseTypes.First(HasGeneratorInterface);
    }

    private static bool HasGeneratorInterface(ITypeSymbol t)
    {
        return t.Interfaces.Select(i => i.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)).Contains(WellKnownFullyQualifiedClassNames.IDataSourceGeneratorAttribute.WithGlobalPrefix);
    }
}