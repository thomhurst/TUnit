using System.Collections.Immutable;
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

    public static string GetTestId(TestGenerationContext testGenerationContext)
    {
        var stringBuilder = new StringBuilder();
        
        if (testGenerationContext.ClassDataSourceAttribute != null)
        {
            stringBuilder.Append(testGenerationContext.ClassDataSourceAttribute.AttributeClass!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            
            if (testGenerationContext.ClassDataAttributeIndex != null)
            {
                stringBuilder.Append($":CDAI{testGenerationContext.ClassDataAttributeIndex}");
            }
            
            if (testGenerationContext.HasEnumerableClassMethodData)
            {
                stringBuilder.Append($":ECMD{{{VariableNames.EnumerableClassDataIndex}}}");
            }

            stringBuilder.Append(':');
        }
        
        if (testGenerationContext.TestDataAttribute != null)
        {
            stringBuilder.Append(testGenerationContext.TestDataAttribute.AttributeClass!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            
            if (testGenerationContext.TestDataAttributeIndex != null)
            {
                stringBuilder.Append($":TDAI{testGenerationContext.TestDataAttributeIndex}");
            }
            
            if (testGenerationContext.HasEnumerableTestMethodData)
            {
                stringBuilder.Append($":ETMD{{{VariableNames.EnumerableTestDataIndex}}}");
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
}