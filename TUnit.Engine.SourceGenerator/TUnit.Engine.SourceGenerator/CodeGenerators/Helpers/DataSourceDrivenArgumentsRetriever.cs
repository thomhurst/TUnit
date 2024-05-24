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
        IMethodSymbol methodSymbol,
        ImmutableArray<AttributeData> methodAttributes,
        AttributeData[] testAndClassAttributes)
    {
        var methodDataIndex = 0;
        foreach (var attributeData in methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                                  == WellKnownFullyQualifiedClassNames
                                                                      .MethodDataSourceAttribute.WithGlobalPrefix))
        {
            var methodData = ParseMethodData(namedTypeSymbol, methodSymbol, attributeData);
            var arguments = methodData.WithTimeoutArgument(testAndClassAttributes);
            yield return new ArgumentsContainer
            {
                DataAttribute = attributeData,
                DataAttributeIndex = ++methodDataIndex,
                IsEnumerableData = false,
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
                IsEnumerableData = true,
                Arguments = [..arguments]
            };
        }

        foreach (var attributeData in methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                                  == WellKnownFullyQualifiedClassNames
                                                                      .ClassDataSourceAttribute.WithGlobalPrefix))
        {
            var classData = ParseClassData(attributeData);
            var arguments = classData.WithTimeoutArgument(testAndClassAttributes);
            yield return new ArgumentsContainer
            {
                DataAttribute = attributeData,
                DataAttributeIndex = ++methodDataIndex,
                IsEnumerableData = false,
                Arguments = [..arguments]
            };
        }
    }

    private static IEnumerable<Argument> ParseMethodData(INamedTypeSymbol namedTypeSymbol, IMethodSymbol methodSymbol, AttributeData methodDataAttribute)
    {
        string methodInvocation;
        
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            var typeContainingMethod = namedTypeSymbol.ToDisplayString(DisplayFormats
                .FullyQualifiedGenericWithGlobalPrefix);

            methodInvocation = $"{typeContainingMethod}.{methodDataAttribute.ConstructorArguments[0].Value!}()";
        }
        else
        {
            var type = ((INamedTypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!)
                .ToDisplayString(DisplayFormats
                    .FullyQualifiedGenericWithGlobalPrefix);
            
            methodInvocation = $"{type}.{methodDataAttribute.ConstructorArguments[1].Value!}()";
        }

        var unfoldTupleArgument = methodDataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "UnfoldTuple");
        
        if (unfoldTupleArgument.Value.Value is true)
        {
            yield return new Argument(ArgumentSource.MethodDataSourceAttribute, "var", methodInvocation);

            for (var index = 1; index <= methodSymbol.Parameters.Length; index++)
            {
                var methodSymbolParameter = methodSymbol.Parameters[index - 1];

                if (index == methodSymbol.Parameters.Length &&
                    methodSymbolParameter.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) ==
                    WellKnownFullyQualifiedClassNames.CancellationToken)
                {
                    break;
                }

                yield return new Argument(ArgumentSource.MethodDataSourceAttribute,
                    methodSymbolParameter.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                    $"{VariableNames.MethodArg}0.Item{index}", isTuple: true);
            }

            yield break;
        }
        
        yield return new Argument(ArgumentSource.MethodDataSourceAttribute, "var", methodInvocation);
    }
    
    private static IEnumerable<Argument> ParseEnumerableMethodData(INamedTypeSymbol namedTypeSymbol,
        AttributeData methodDataAttribute)
    {
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            var typeContainingMethod = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
            return [new Argument(ArgumentSource.EnumerableMethodDataAttribute, "var", $"{typeContainingMethod}.{methodDataAttribute.ConstructorArguments.SafeFirstOrDefault().Value!}()")];
        }

        var type = ((INamedTypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!)
            .ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        return [new Argument(ArgumentSource.MethodDataSourceAttribute, "var", $"{type}.{methodDataAttribute.ConstructorArguments[1].Value!}()")];
    }
    
    private static IEnumerable<Argument> ParseClassData(AttributeData classDataAttribute)
    {
        var type = (INamedTypeSymbol)classDataAttribute.ConstructorArguments[0].Value!;
        var fullyQualifiedType = type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        return [new Argument(ArgumentSource.ClassDataSourceAttribute, fullyQualifiedType, $"new {fullyQualifiedType}()")];
    }
}