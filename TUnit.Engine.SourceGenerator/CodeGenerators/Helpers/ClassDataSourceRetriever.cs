using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class ClassDataSourceRetriever
{
    public static ArgumentsContainer ParseClassData(INamedTypeSymbol namedTypeSymbol, AttributeData classDataAttribute, ArgumentsType argumentsType, int index)
    {
        var genericType = classDataAttribute.AttributeClass!.TypeArguments.FirstOrDefault() ?? (INamedTypeSymbol)classDataAttribute.ConstructorArguments[0].Value!;
        var fullyQualifiedGenericType = genericType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        var sharedArgument = classDataAttribute.NamedArguments.SafeFirstOrDefault(x => x.Key == "Shared").Value;

        var sharedArgumentType = sharedArgument.ToCSharpString();

        var key = classDataAttribute.NamedArguments.SafeFirstOrDefault(x => x.Key == "Key").Value.Value as string;
            
        return new ClassDataSourceAttributeContainer
        {
            Attribute = classDataAttribute,
            AttributeIndex = index,
            Key = key,
            ForClass = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            ArgumentsType = argumentsType,
            SharedArgumentType = sharedArgumentType,
            TypeName = fullyQualifiedGenericType,
            DisposeAfterTest = classDataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ?? true,
        };
    }
}