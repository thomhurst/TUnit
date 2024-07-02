using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TestInformationRetriever
{
    public static int GetRepeatCount(AttributeData[] methodAndClassAttributes)
    {
        return methodAndClassAttributes
            .SafeFirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                     == "global::TUnit.Core.RepeatAttribute")
            ?.ConstructorArguments.SafeFirstOrDefault().Value as int? ?? 0;
    }
    
    public static TestLocation GetTestLocation(AttributeData[] methodAndClassAttributes)
    {
        var testAttribute = methodAndClassAttributes
            .First(x => x.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames.BaseTestAttribute.WithGlobalPrefix) == true);

        return new TestLocation
        {
            FilePath = TypedConstantParser.GetTypedConstantValue(testAttribute.ConstructorArguments[0])!,
            LineNumber = int.Parse(TypedConstantParser.GetTypedConstantValue(testAttribute.ConstructorArguments[1])!)
        };
    }

    public static string GetTestId(TestGenerationContext testGenerationContext)
    {
        var stringBuilder = new StringBuilder();
        
        if (testGenerationContext.ClassDataSourceAttribute != null)
        {
            stringBuilder.Append(testGenerationContext.ClassDataSourceAttribute.AttributeClass!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            
            if (testGenerationContext.ClassDataAttributeIndex != null)
            {
                stringBuilder.Append($":{testGenerationContext.ClassDataAttributeIndex}");
            }
            
            if (testGenerationContext.HasEnumerableClassMethodData)
            {
                stringBuilder.Append($":{{{VariableNames.EnumerableClassDataIndex}}}");
            }

            stringBuilder.Append(':');
        }
        
        if (testGenerationContext.TestDataAttribute != null)
        {
            stringBuilder.Append(testGenerationContext.TestDataAttribute.AttributeClass!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            
            if (testGenerationContext.TestDataAttributeIndex != null)
            {
                stringBuilder.Append($":{testGenerationContext.TestDataAttributeIndex}");
            }
            
            if (testGenerationContext.HasEnumerableTestMethodData)
            {
                stringBuilder.Append($":{{{VariableNames.EnumerableTestDataIndex}}}");
            }
        }
        else
        {
            stringBuilder.Append(testGenerationContext.TestAttribute.AttributeClass!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        }
        
        stringBuilder.Append(':');
        
        var fullyQualifiedClassName =
            testGenerationContext.ClassSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix);

        stringBuilder.Append(fullyQualifiedClassName);
        stringBuilder.Append('.');
        
        var testName = testGenerationContext.MethodSymbol.Name;
        
        stringBuilder.Append(testName);
        stringBuilder.Append(':');
        
        var classParameters = testGenerationContext.ClassSymbol.Constructors.SafeFirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        
        var classParameterTypes = GetTypes(classParameters);

        if (!string.IsNullOrEmpty(classParameterTypes))
        {
            stringBuilder.Append(classParameterTypes);
            stringBuilder.Append(':');
        }

        var methodParameterTypes = GetTypes(testGenerationContext.MethodSymbol.Parameters);

        if (!string.IsNullOrEmpty(methodParameterTypes))
        {
            stringBuilder.Append(methodParameterTypes);
            stringBuilder.Append(':');
        }
        
        stringBuilder.Append(testGenerationContext.CurrentRepeatAttempt);
        
        return stringBuilder.ToString();
    }

    private static string GetArgumentSourcePrefix(IEnumerable<Argument> testArguments)
    {
        if (testArguments.SafeFirstOrDefault()?.ArgumentSource is { } argumentSource)
        {
            return $"{argumentSource}:";
        }
        
        return string.Empty;
    }

    public static string GetTypes(ImmutableArray<IParameterSymbol> parameters)
    {
        if (parameters.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var parameterTypesFullyQualified = parameters.Select(x => x.Type)
            .Select(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix));
        
        return $"({string.Join(",", parameterTypesFullyQualified)})";
    }

    public static string GetReturnType(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
        if (returnType == "global::System.Void")
        {
            return "typeof(void)";
        }

        return $"typeof({returnType})";
    }
}