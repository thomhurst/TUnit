using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        AttributeData[] testAndClassAttributes,
        string argPrefix)
    {
        var methodDataIndex = 0;
        foreach (var attributeData in methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                                  == WellKnownFullyQualifiedClassNames
                                                                      .MethodDataSourceAttribute.WithGlobalPrefix))
        {
            var methodData = ParseMethodData(namedTypeSymbol, methodSymbol, attributeData, argPrefix);
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
            var methodData = ParseEnumerableMethodData(namedTypeSymbol, methodSymbol, attributeData, argPrefix);
            var arguments = methodData.WithTimeoutArgument(testAndClassAttributes);
            yield return new ArgumentsContainer
            {
                DataAttribute = attributeData,
                DataAttributeIndex = ++methodDataIndex,
                IsEnumerableData = true,
                Arguments = [..arguments]
            };
        }

        foreach (var classDataAttribute in methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                                       == WellKnownFullyQualifiedClassNames
                                                                           .ClassDataSourceAttribute.WithGlobalPrefix))
        {
            var genericType = classDataAttribute.AttributeClass!.TypeArguments.FirstOrDefault() ?? (INamedTypeSymbol)classDataAttribute.ConstructorArguments[0].Value!;
            var fullyQualifiedGenericType = genericType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
            var sharedArgument = classDataAttribute.NamedArguments.SafeFirstOrDefault(x => x.Key == "Shared").Value;

            var sharedArgumentType = sharedArgument.ToCSharpString();

            var key = classDataAttribute.NamedArguments.SafeFirstOrDefault(x => x.Key == "Key").Value.Value as string;
            var arguments = GetClassDataArguments(sharedArgumentType, key ?? string.Empty, namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix), fullyQualifiedGenericType);
            
            yield return new ArgumentsContainer
            {
                DataAttribute = classDataAttribute,
                DataAttributeIndex = ++methodDataIndex,
                IsEnumerableData = false,
                Arguments = [..arguments.WithTimeoutArgument(testAndClassAttributes)]
            };
        }
    }

    private static IEnumerable<Argument> GetClassDataArguments(string sharedArgumentType, string? key, string className, string fullyQualifiedGenericType)
    {
            if (sharedArgumentType is "TUnit.Core.SharedType.Globally")
            {
                return
                [
                    new Argument(ArgumentSource.ClassDataSourceAttribute, fullyQualifiedGenericType,
                        $"({fullyQualifiedGenericType})global::TUnit.Engine.TestDataContainer.InjectedSharedGlobally.GetOrAdd(typeof({fullyQualifiedGenericType}), x => new {fullyQualifiedGenericType}())")
                ];
            }
            
            if (sharedArgumentType is "TUnit.Core.SharedType.ForClass")
            {
                return
                [
                    new Argument(ArgumentSource.ClassDataSourceAttribute, fullyQualifiedGenericType,
                        $"({fullyQualifiedGenericType})global::TUnit.Engine.TestDataContainer.InjectedSharedPerClassType.GetOrAdd(new global::TUnit.Engine.Models.DictionaryTypeTypeKey(typeof({className}), typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())")
                ];
            }
            
            if (sharedArgumentType is "TUnit.Core.SharedType.Keyed")
            {
                return
                [
                    new Argument(ArgumentSource.ClassDataSourceAttribute, fullyQualifiedGenericType,
                        $"({fullyQualifiedGenericType})global::TUnit.Engine.TestDataContainer.InjectedSharedPerKey.GetOrAdd(new global::TUnit.Engine.Models.DictionaryStringTypeKey(\"{key}\", typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())")
                ];
            }

            return
            [
                new Argument(ArgumentSource.ClassDataSourceAttribute, fullyQualifiedGenericType,
                    $"new {fullyQualifiedGenericType}()")
            ];
    }

    public static IEnumerable<Argument> ParseMethodData(INamedTypeSymbol namedTypeSymbol, IMethodSymbol methodSymbol, AttributeData methodDataAttribute, string argPrefix)
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

            var variableNames = methodSymbol.ParametersWithoutTimeoutCancellationToken().Select((x, i) => $"{argPrefix}{i+1}").ToList();
            
            yield return new Argument(ArgumentSource.MethodDataSourceAttribute,
                "var",
                $"{argPrefix}0", isTuple: true)
            {
                TupleVariableNames = $"({string.Join(", ", variableNames)})"
            };

            yield break;
        }
        
        yield return new Argument(ArgumentSource.MethodDataSourceAttribute, "var", methodInvocation);
    }
    
    public static IEnumerable<Argument> ParseEnumerableMethodData(INamedTypeSymbol namedTypeSymbol,
        IMethodSymbol methodSymbol,
        AttributeData methodDataAttribute, 
        string argPrefix)
    {
        string methodInvocation;
        
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            var typeContainingMethod = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
            
            methodInvocation = $"{typeContainingMethod}.{methodDataAttribute.ConstructorArguments.SafeFirstOrDefault().Value!}()";
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
            yield return new Argument(ArgumentSource.EnumerableMethodDataAttribute, "var", methodInvocation);

            var variableNames = methodSymbol.ParametersWithoutTimeoutCancellationToken().Select((x, i) => $"{argPrefix}{i+1}").ToList();
            
            yield return new Argument(ArgumentSource.EnumerableMethodDataAttribute,
                "var",
                $"{argPrefix}0", isTuple: true)
            {
                TupleVariableNames = $"({string.Join(", ", variableNames)})"
            };

            yield break;
        }
        
        yield return new Argument(ArgumentSource.EnumerableMethodDataAttribute, "var", methodInvocation);
    }
}