using System.Collections.Immutable;
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
            .SafeFirstOrDefault(x => x.AttributeClass?.GloballyQualifiedNonGeneric()
                                     == "global::TUnit.Core.RepeatAttribute")
            ?.ConstructorArguments.SafeFirstOrDefault().Value as int? ?? 0;
    }

    public static string GetTestId(TestGenerationContext testGenerationContext)
    {
        using var writer = new SourceCodeWriter(1); // Inline expression

        if (testGenerationContext.ClassArguments is DataSourceAttributeContainer { Attribute.AttributeClass: not null } classDataAttributeContainer)
        {
            writer.Write($"{classDataAttributeContainer.Attribute.AttributeClass?.GloballyQualified()}:{{{VariableNames.ClassDataIndex}}}:");
        }

        if (testGenerationContext.ClassArguments is MethodDataSourceAttributeContainer classLevelMethodDataSourceAttributeContainer)
        {
            writer.Write(classLevelMethodDataSourceAttributeContainer.IsExpandableEnumerable
                ? $"CL-EMDS{classLevelMethodDataSourceAttributeContainer.AttributeIndex}:{{{VariableNames.ClassDataIndex}}}:"
                : $"CL-MDS{classLevelMethodDataSourceAttributeContainer.AttributeIndex}:");
        }

        if (testGenerationContext.ClassArguments is ArgumentsAttributeContainer classLevelArgumentsContainer)
        {
            writer.Write($"CL-ARGS{classLevelArgumentsContainer.AttributeIndex}:");
        }
        
        if (testGenerationContext.ClassArguments is ClassConstructorAttributeContainer classLevelClassConstructorAttribute)
        {
            writer.Write($"CL-CCA{classLevelClassConstructorAttribute.AttributeIndex}:");
        }
        
        if (testGenerationContext.ClassArguments is AsyncDataSourceGeneratorContainer classLevelGeneratedArgumentsContainer)
        {
            writer.Write($"CL-GAC{classLevelGeneratedArgumentsContainer.AttributeIndex}:");
        }
        
        if (testGenerationContext.TestArguments is DataSourceAttributeContainer { Attribute.AttributeClass: not null } testMethodDataAttributeContainer)
        {
            writer.Write($"{testMethodDataAttributeContainer.Attribute.AttributeClass?.GloballyQualified()}:{{{VariableNames.TestMethodDataIndex}}}:");
        }

        if (testGenerationContext.TestArguments is MethodDataSourceAttributeContainer testLevelMethodDataSourceAttributeContainer)
        {
            writer.Write(testLevelMethodDataSourceAttributeContainer.IsExpandableEnumerable
                ? $"TL-EMDS{testLevelMethodDataSourceAttributeContainer.AttributeIndex}:{{{VariableNames.TestMethodDataIndex}}}:"
                : $"TL-MDS{testLevelMethodDataSourceAttributeContainer.AttributeIndex}:");
        }

        if (testGenerationContext.TestArguments is ArgumentsAttributeContainer testLevelArgumentsContainer)
        {
            writer.Write($"TL-ARGS{testLevelArgumentsContainer.AttributeIndex}:");
        }
        
        if (testGenerationContext.TestArguments is AsyncDataSourceGeneratorContainer testLevelGeneratedArgumentsContainer)
        {
            writer.Write($"TL-GAC{testLevelGeneratedArgumentsContainer.AttributeIndex}:");
        }
        
        var fullyQualifiedClassName =
            testGenerationContext.ClassSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix);

        writer.Write(fullyQualifiedClassName);
        
        var classParameters = testGenerationContext.ClassSymbol.Constructors.SafeFirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        
        var classParameterTypes = GetTypes(classParameters);

        if (!string.IsNullOrEmpty(classParameterTypes))
        {
            writer.Write(classParameterTypes);
        }
        
        writer.Write(".");
        
        var testName = testGenerationContext.MethodSymbol.Name;
        
        writer.Write(testName);

        var methodParameterTypes = GetTypes(testGenerationContext.MethodSymbol.Parameters);

        if (!string.IsNullOrEmpty(methodParameterTypes))
        {
            writer.Write(methodParameterTypes);
        }
        
        writer.Write(":");
        
        writer.Write(testGenerationContext.CurrentRepeatAttempt.ToString());
        
        return writer.ToString().Trim();
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