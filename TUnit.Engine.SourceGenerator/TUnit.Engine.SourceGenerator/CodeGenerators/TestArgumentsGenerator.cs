using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

public static class TestArgumentsGenerator
{
    public static IEnumerable<string> GetTestMethodArguments(IMethodSymbol methodSymbol, AttributeData testAttribute)
    {
        AttributeData[] attributes =
        [
            ..methodSymbol.GetAttributes(),
            ..methodSymbol.ContainingType.GetAttributes(),
        ];

        var timeoutAttribute = attributes.FirstOrDefault(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            == WellKnownFullyQualifiedClassNames.TimeoutAttribute);

        switch (testAttribute.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix))
        {
            case WellKnownFullyQualifiedClassNames.TestAttribute:
                // Basic Test Attributes don't take arguments
                break;
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
            case "global::TUnit.Core.CombinativeTestAttribute":
                break;
        }
        
        if (timeoutAttribute != null)
        {
            var timeoutInMillis = (int) timeoutAttribute.ConstructorArguments.First().Value!;
            yield return $"global::TUnit.Engine.EngineCancellationToken.CreateToken(global::System.TimeSpan.FromMilliseconds({timeoutInMillis}))";
        }
    }

    private static string GetMethodData(AttributeData methodData)
    {
        if (methodData.ConstructorArguments.Length == 1)
        {
            return methodData.ConstructorArguments.First().Value!.ToString();
        }

        var type = (INamedTypeSymbol)methodData.ConstructorArguments[0].Value!;
        return $"{type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}.{methodData.ConstructorArguments[1].Value!}";
    }
    
    private static string GetClassData(AttributeData methodData)
    {
        var type = (INamedTypeSymbol)methodData.ConstructorArguments[0].Value!;
        return $"new {type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}()";
    }
    
    private static IEnumerable<string> GetDataDrivenTestArguments(AttributeData argumentsAttribute)
    {
        return argumentsAttribute.ConstructorArguments
            .Select(GetTypedConstantValue);
    }

    private static string GetTypedConstantValue(TypedConstant constructorArgument)
    {
        return constructorArgument.Kind switch
        {
            TypedConstantKind.Error => "null",
            TypedConstantKind.Primitive => $"{constructorArgument.Value}",
            TypedConstantKind.Enum => $"{constructorArgument.Value}",
            TypedConstantKind.Type => $"{constructorArgument.Value}",
            TypedConstantKind.Array =>
                $"[{string.Join(",", constructorArgument.Values.Select(GetTypedConstantValue))}]",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}