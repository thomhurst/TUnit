using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class DataSourceDrivenArgumentsRetriever
{
    public static IEnumerable<ArgumentsContainer> Parse(
        INamedTypeSymbol namedTypeSymbol,
        ImmutableArray<AttributeData> methodAttributes,
        AttributeData[] testAndClassAttributes)
    {
        var methodDataIndex = 0;
        foreach (var attributeData in methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                                  == WellKnownFullyQualifiedClassNames
                                                                      .MethodDataAttribute.WithGlobalPrefix))
        {
            var methodData = ParseMethodData(namedTypeSymbol, attributeData);
            var arguments = methodData.WithTimeoutArgument(testAndClassAttributes);
            yield return new ArgumentsContainer
            {
                DataAttribute = attributeData,
                DataAttributeIndex = ++methodDataIndex,
                Arguments = [..arguments]
            };
        }

        foreach (var attributeData in methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                                  == WellKnownFullyQualifiedClassNames
                                                                      .EnumerableMethodDataAttribute.WithGlobalPrefix))
        {
            var methodData = ParseEnumerableMethodData(namedTypeSymbol, attributeData);
            var arguments = methodData.WithTimeoutArgument(testAndClassAttributes);
            yield return new ArgumentsContainer
            {
                DataAttribute = attributeData,
                DataAttributeIndex = ++methodDataIndex,
                Arguments = [..arguments]
            };
        }

        foreach (var attributeData in methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                                  == WellKnownFullyQualifiedClassNames
                                                                      .ClassDataAttribute.WithGlobalPrefix))
        {
            var classData = ParseClassData(attributeData);
            var arguments = classData.WithTimeoutArgument(testAndClassAttributes);
            yield return new ArgumentsContainer
            {
                DataAttribute = attributeData,
                DataAttributeIndex = ++methodDataIndex,
                Arguments = [..arguments]
            };
        }
    }

    private static IEnumerable<Argument> ParseMethodData(INamedTypeSymbol namedTypeSymbol,
        AttributeData methodDataAttribute)
    {
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            var typeContainingMethod = namedTypeSymbol.ToDisplayString(DisplayFormats
                .FullyQualifiedGenericWithGlobalPrefix);
            return [new Argument(ArgumentSource.MethodDataAttribute, "var", $"{typeContainingMethod}.{methodDataAttribute.ConstructorArguments.First().Value!}()")];
        }

        var type = ((INamedTypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!)
            .ToDisplayString(DisplayFormats
            .FullyQualifiedGenericWithGlobalPrefix);
        return [new Argument(ArgumentSource.MethodDataAttribute, "var", $"{type}.{methodDataAttribute.ConstructorArguments[1].Value!}()")];
    }
    
    private static IEnumerable<Argument> ParseEnumerableMethodData(INamedTypeSymbol namedTypeSymbol,
        AttributeData methodDataAttribute)
    {
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            var typeContainingMethod = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
            return [new Argument(ArgumentSource.EnumerableMethodDataAttribute, "var", $"{typeContainingMethod}.{methodDataAttribute.ConstructorArguments.First().Value!}()")];
        }

        var type = ((INamedTypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!)
            .ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        return [new Argument(ArgumentSource.MethodDataAttribute, "var", $"{type}.{methodDataAttribute.ConstructorArguments[1].Value!}()")];
    }
    
    private static IEnumerable<Argument> ParseClassData(AttributeData classDataAttribute)
    {
        var type = (INamedTypeSymbol)classDataAttribute.ConstructorArguments[0].Value!;
        var fullyQualifiedType = type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        return [new Argument(ArgumentSource.ClassDataAttribute, fullyQualifiedType, $"new {fullyQualifiedType}()")];
    }
}