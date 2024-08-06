using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class MethodDataSourceRetriever
{
    public static ArgumentsContainer ParseMethodData(ImmutableArray<IParameterSymbol> parameters,
        INamedTypeSymbol namedTypeSymbol, AttributeData attributeData, string argPrefix, int index)
    {
        var arguments = ParseMethodDataArguments(parameters, namedTypeSymbol, attributeData, argPrefix);
        
        return new ArgumentsContainer
        {
            DataAttribute = attributeData,
            DataAttributeIndex = index,
            IsEnumerableData = false,
            Arguments = [..arguments]
        };
    }
    
    public static ArgumentsContainer ParseEnumerableMethodData(ImmutableArray<IParameterSymbol> parameters,
        INamedTypeSymbol namedTypeSymbol, AttributeData attributeData, string argPrefix, int index)
    {
        var arguments = ParseEnumerableMethodDataArguments(parameters, namedTypeSymbol, attributeData, argPrefix);
        
        return new ArgumentsContainer
        {
            DataAttribute = attributeData,
            DataAttributeIndex = index,
            IsEnumerableData = true,
            Arguments = [..arguments]
        };
    }

    private static IEnumerable<Argument> ParseMethodDataArguments(ImmutableArray<IParameterSymbol> parameters, INamedTypeSymbol namedTypeSymbol, AttributeData methodDataAttribute, string argPrefix)
    {
        string methodInvocation;
        IMethodSymbol? dataSourceMethod;
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            var typeContainingMethod = namedTypeSymbol.ToDisplayString(DisplayFormats
                .FullyQualifiedGenericWithGlobalPrefix);

            dataSourceMethod = namedTypeSymbol.GetMembers(methodDataAttribute.ConstructorArguments[0].Value!.ToString())
                .OfType<IMethodSymbol>().First();
            
            methodInvocation = dataSourceMethod.IsStatic 
                ? $"{typeContainingMethod}.{methodDataAttribute.ConstructorArguments.SafeFirstOrDefault().Value!}()" 
                : $"resettableClassFactory.Value.{methodDataAttribute.ConstructorArguments.SafeFirstOrDefault().Value!}()";
        }
        else
        {
            var typeContainingDataSourceMethod = (INamedTypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!;
            var type = typeContainingDataSourceMethod.ToDisplayString(DisplayFormats
                    .FullyQualifiedGenericWithGlobalPrefix);
            
            methodInvocation = $"{type}.{methodDataAttribute.ConstructorArguments[1].Value!}()";
            
            dataSourceMethod = typeContainingDataSourceMethod.GetMembers(methodDataAttribute.ConstructorArguments[1].Value!.ToString())
                .OfType<IMethodSymbol>().First();
        }

        var disposeAfterTest = methodDataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ?? true;

        var unfoldTupleArgument = methodDataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "UnfoldTuple");
        
        if (unfoldTupleArgument.Value.Value is true)
        {
            var variableNames = parameters.Select((x, i) => $"{argPrefix}{i}").ToArray();
            
            yield return new Argument(dataSourceMethod.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix), methodInvocation, isTuple: true)
            {
                TupleVariableNames = variableNames,
                DisposeAfterTest = disposeAfterTest
            };

            yield break;
        }
        
        yield return new Argument(dataSourceMethod.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix), methodInvocation)
        {
            DisposeAfterTest = disposeAfterTest
        };
    }
    
    private static IEnumerable<Argument> ParseEnumerableMethodDataArguments(
        ImmutableArray<IParameterSymbol> parameters,
        INamedTypeSymbol namedTypeSymbol,
        AttributeData methodDataAttribute, 
        string argPrefix)
    {
        string methodInvocation;
        IMethodSymbol? dataSourceMethod;
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            var typeContainingMethod = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
            
            dataSourceMethod = namedTypeSymbol.GetMembers(methodDataAttribute.ConstructorArguments[0].Value!.ToString())
                .OfType<IMethodSymbol>().First();
            
            methodInvocation = dataSourceMethod.IsStatic 
                ? $"{typeContainingMethod}.{methodDataAttribute.ConstructorArguments.SafeFirstOrDefault().Value!}()" 
                : $"resettableClassFactory.Value.{methodDataAttribute.ConstructorArguments.SafeFirstOrDefault().Value!}()";
        }
        else
        {
            var typeContainingDataSourceMethod = ((INamedTypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!);
            var type = typeContainingDataSourceMethod
                .ToDisplayString(DisplayFormats
                    .FullyQualifiedGenericWithGlobalPrefix);
            
            dataSourceMethod = typeContainingDataSourceMethod.GetMembers(methodDataAttribute.ConstructorArguments[1].Value!.ToString())
                .OfType<IMethodSymbol>().First();
            
            methodInvocation = $"{type}.{methodDataAttribute.ConstructorArguments[1].Value!}()";
        }

        var disposeAfterTest = methodDataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ?? true;
        
        var unfoldTupleArgument = methodDataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "UnfoldTuple");
        
        if (unfoldTupleArgument.Value.Value is true)
        {
            var variableNames = parameters.Select((x, i) => $"{argPrefix}{i}").ToArray();
            
            yield return new Argument(dataSourceMethod.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix), methodInvocation, isTuple: true)
            {
                TupleVariableNames = variableNames,
                DisposeAfterTest = disposeAfterTest
            };

            yield break;
        }

        yield return new Argument(dataSourceMethod.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            methodInvocation)
        {
            DisposeAfterTest = disposeAfterTest
        };
    }
}