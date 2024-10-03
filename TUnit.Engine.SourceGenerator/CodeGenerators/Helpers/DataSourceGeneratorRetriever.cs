using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class DataSourceGeneratorRetriever
{
    public static ArgumentsContainer Parse(ImmutableArray<IParameterSymbol> parameters,
        INamedTypeSymbol namedTypeSymbol, AttributeData attributeData, ArgumentsType argumentsType, int index)
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
                attributeData.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ??
                true,
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