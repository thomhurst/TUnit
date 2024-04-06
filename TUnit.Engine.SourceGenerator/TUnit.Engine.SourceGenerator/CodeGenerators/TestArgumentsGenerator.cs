using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class TestArgumentsGenerator
{
    public static IEnumerable<Argument> GetTestMethodArguments(IMethodSymbol methodSymbol, AttributeData testAttribute)
    {
        if (!methodSymbol.Parameters.Any())
        {
            yield return Argument.NoArguments;
        }
        
        switch (testAttribute.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix))
        {
            case WellKnownFullyQualifiedClassNames.ArgumentsAttribute:
                foreach (var dataDrivenTestArgument in GetDataDrivenTestArguments(testAttribute))
                {
                    yield return dataDrivenTestArgument;
                }
                break;
            case WellKnownFullyQualifiedClassNames.MethodDataAttribute:
                yield return GetMethodData(testAttribute);
                break;
            case WellKnownFullyQualifiedClassNames.ClassDataAttribute:
                yield return GetClassData(testAttribute);
                break;
            case WellKnownFullyQualifiedClassNames.CombinativeValuesAttribute:
                break;
        }

        var timeoutCancellationTokenArgument =
            TimeoutCancellationTokenGenerator.GetCancellationTokenArgument(methodSymbol);
        
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