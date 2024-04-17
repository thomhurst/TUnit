using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TestArgumentsRetriever
{
    public static IEnumerable<Argument> GetTestMethodArguments(IMethodSymbol methodSymbol, AttributeData testAttribute, AttributeData[] methodAndClassAttributes)
    {
        if (methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            yield break;
        }

        var attributeTypeName = testAttribute.GetFullyQualifiedAttributeTypeName();
        
        if (attributeTypeName == WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithGlobalPrefix)
        {
            foreach (var dataDrivenTestArgument in GetDataDrivenTestArguments(testAttribute))
            {
                yield return dataDrivenTestArgument;
            }
        }
        else if (attributeTypeName == WellKnownFullyQualifiedClassNames.MethodDataAttribute.WithGlobalPrefix)
        {
            yield return GetMethodData(testAttribute);
        }
        else if (attributeTypeName == WellKnownFullyQualifiedClassNames.ClassDataAttribute.WithGlobalPrefix)
        {
            yield return GetClassData(testAttribute);
        }
        else if (attributeTypeName == WellKnownFullyQualifiedClassNames.CombinativeValuesAttribute.WithGlobalPrefix)
        {
        }

        var timeoutCancellationTokenArgument =
            TimeoutCancellationTokenRetriever.GetCancellationTokenArgument(methodAndClassAttributes);
        
        if (timeoutCancellationTokenArgument != null)
        {
            yield return timeoutCancellationTokenArgument;
        }
    }

    private static Argument GetMethodData(AttributeData methodData)
    {
        if (methodData.ConstructorArguments.Length == 1)
        {
            return new Argument("var", $"{methodData.ConstructorArguments.First().Value!}()");
        }

        var type = (INamedTypeSymbol)methodData.ConstructorArguments[0].Value!;
        return new Argument("var", $"{type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}.{methodData.ConstructorArguments[1].Value!}()");
    }
    
    private static Argument GetClassData(AttributeData methodData)
    {
        var type = (INamedTypeSymbol)methodData.ConstructorArguments[0].Value!;
        return new Argument("var", $"new {type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}()");
    }
    
    private static IEnumerable<Argument> GetDataDrivenTestArguments(AttributeData argumentsAttribute)
    {
        foreach (var typedConstant in argumentsAttribute.ConstructorArguments.First().Values)
        {
            var type = TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(typedConstant);
            var value = TypedConstantParser.GetTypedConstantValue(typedConstant);

            yield return new Argument(type, value);
        }
    }
}