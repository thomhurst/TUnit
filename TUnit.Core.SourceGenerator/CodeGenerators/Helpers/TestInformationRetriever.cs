using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TestInformationRetriever
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

        if (testGenerationContext.ClassArguments is DataAttributeContainer { Attribute.AttributeClass: not null } classDataAttributeContainer)
        {
            stringBuilder.Append($"{classDataAttributeContainer.Attribute.AttributeClass?.GloballyQualified()}:{{{VariableNames.ClassDataIndex}}}:");
        }

        if (testGenerationContext.ClassArguments is MethodDataSourceAttributeContainer classLevelMethodDataSourceAttributeContainer)
        {
            stringBuilder.Append(classLevelMethodDataSourceAttributeContainer.IsEnumerableData
                ? $"CL-EMDS{classLevelMethodDataSourceAttributeContainer.AttributeIndex}:{{{VariableNames.ClassDataIndex}}}:"
                : $"CL-MDS{classLevelMethodDataSourceAttributeContainer.AttributeIndex}:");
        }

        if (testGenerationContext.ClassArguments is ArgumentsAttributeContainer classLevelArgumentsContainer)
        {
            stringBuilder.Append($"CL-ARGS{classLevelArgumentsContainer.AttributeIndex}:");
        }
        
        if (testGenerationContext.ClassArguments is ClassConstructorAttributeContainer classLevelClassConstructorAttribute)
        {
            stringBuilder.Append($"CL-CCA{classLevelClassConstructorAttribute.AttributeIndex}:");
        }
        
        if (testGenerationContext.ClassArguments is GeneratedArgumentsContainer classLevelGeneratedArgumentsContainer)
        {
            stringBuilder.Append($"CL-GAC{classLevelGeneratedArgumentsContainer.AttributeIndex}:");
        }
        
        if (testGenerationContext.TestArguments is DataAttributeContainer { Attribute.AttributeClass: not null } testMethodDataAttributeContainer)
        {
            stringBuilder.Append($"{testMethodDataAttributeContainer.Attribute.AttributeClass?.GloballyQualified()}:{{{VariableNames.TestMethodDataIndex}}}:");
        }

        if (testGenerationContext.TestArguments is MethodDataSourceAttributeContainer testLevelMethodDataSourceAttributeContainer)
        {
            stringBuilder.Append(testLevelMethodDataSourceAttributeContainer.IsEnumerableData
                ? $"TL-EMDS{testLevelMethodDataSourceAttributeContainer.AttributeIndex}:{{{VariableNames.TestMethodDataIndex}}}:"
                : $"TL-MDS{testLevelMethodDataSourceAttributeContainer.AttributeIndex}:");
        }

        if (testGenerationContext.TestArguments is ArgumentsAttributeContainer testLevelArgumentsContainer)
        {
            stringBuilder.Append($"TL-ARGS{testLevelArgumentsContainer.AttributeIndex}:");
        }
        
        if (testGenerationContext.TestArguments is GeneratedArgumentsContainer testLevelGeneratedArgumentsContainer)
        {
            stringBuilder.Append($"TL-GAC{testLevelGeneratedArgumentsContainer.AttributeIndex}:");
        }
        
        var fullyQualifiedClassName =
            testGenerationContext.ClassSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix);

        stringBuilder.Append(fullyQualifiedClassName);
        
        var classParameters = testGenerationContext.ClassSymbol.Constructors.SafeFirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        
        var classParameterTypes = GetTypes(classParameters);

        if (!string.IsNullOrEmpty(classParameterTypes))
        {
            stringBuilder.Append(classParameterTypes);
        }
        
        stringBuilder.Append('.');
        
        var testName = testGenerationContext.MethodSymbol.Name;
        
        stringBuilder.Append(testName);

        var methodParameterTypes = GetTypes(testGenerationContext.MethodSymbol.Parameters);

        if (!string.IsNullOrEmpty(methodParameterTypes))
        {
            stringBuilder.Append(methodParameterTypes);
        }
        
        stringBuilder.Append(':');
        
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