using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Executors;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TestSourceDataModelRetriever
{
    public static IEnumerable<TestSourceDataModel> ParseTestDatas(this IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol)
    {
        if (methodSymbol.IsAbstract || namedTypeSymbol.IsAbstract || namedTypeSymbol.IsGenericType)
        {
            yield break;
        }
        
        var testAttribute = methodSymbol.GetRequiredTestAttribute();
        
        var classArgumentsContainers = ArgumentsRetriever.GetArguments(namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty, namedTypeSymbol.GetAttributes().Concat(namedTypeSymbol.ContainingAssembly.GetAttributes().Where(x => x.IsDataSourceAttribute())).ToImmutableArray(), namedTypeSymbol, VariableNames.ClassArg).ToArray();
        var testArgumentsContainers = ArgumentsRetriever.GetArguments(methodSymbol.Parameters, methodSymbol.GetAttributes(), namedTypeSymbol, VariableNames.MethodArg);
        
        var repeatCount =
            TestInformationRetriever.GetRepeatCount(methodSymbol.GetAttributesIncludingClass(namedTypeSymbol));

        var runCount = repeatCount + 1;

        foreach (var testArguments in testArgumentsContainers)
        {
            foreach (var classArguments in classArgumentsContainers)
            {
                foreach (var testSourceDataModel in GenerateTestSourceDataModels(methodSymbol, namedTypeSymbol, classArguments, runCount, testAttribute, testArguments))
                {
                    yield return testSourceDataModel;
                }
            }
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateTestSourceDataModels(IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol,
        ArgumentsContainer classArguments, int runCount, AttributeData testAttribute, ArgumentsContainer testArguments)
    {
        if (!classArguments.HasData())
        {
            foreach (var testSourceDataModel in GenerateSingleClassInstance(methodSymbol, namedTypeSymbol, runCount, testAttribute,
                         testArguments))
            {
                yield return testSourceDataModel;
            }

            yield break;
        }

        foreach (var generateMultipleClassInstance in GenerateMultipleClassInstances(methodSymbol, namedTypeSymbol, runCount, testAttribute,
                     classArguments, testArguments))
        {
            yield return generateMultipleClassInstance;
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateSingleClassInstance(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute,
        ArgumentsContainer testArguments)
    {
        for (var i = 0; i < runCount; i++)
        {
            yield return GetTestSourceDataModel(new TestGenerationContext
            {
                    MethodSymbol = methodSymbol,
                    ClassSymbol = namedTypeSymbol,
                    ClassArguments = [],
                    TestArguments = testArguments.Arguments,
                    ClassDataSourceAttribute = null,
                    TestDataAttribute = testArguments.DataAttribute,
                    CurrentRepeatAttempt = i,
                    TestAttribute = testAttribute,
                    HasEnumerableTestMethodData = testArguments.IsEnumerableData,
                    HasEnumerableClassMethodData = false,
                    ClassDataAttributeIndex = null,
                    TestDataAttributeIndex = testArguments.DataAttributeIndex,
                    ClassConstructorType = null
                });
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateMultipleClassInstances(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute, ArgumentsContainer classArguments,
        ArgumentsContainer testArguments)
    {
        for (var i = 0; i < runCount; i++)
        {
            yield return GetTestSourceDataModel(new TestGenerationContext
            {
                MethodSymbol = methodSymbol,
                ClassSymbol = namedTypeSymbol,
                ClassArguments = classArguments.Arguments,
                TestArguments = testArguments.Arguments,
                ClassDataSourceAttribute = classArguments.DataAttribute,
                TestDataAttribute = testArguments.DataAttribute,
                CurrentRepeatAttempt = i,
                TestAttribute = testAttribute,
                HasEnumerableTestMethodData = testArguments.IsEnumerableData,
                HasEnumerableClassMethodData = classArguments.IsEnumerableData,
                TestDataAttributeIndex = testArguments.DataAttributeIndex,
                ClassDataAttributeIndex = classArguments.DataAttributeIndex, 
                ClassConstructorType = classArguments.ClassConstructorType
            });
        }
    }

    private static TestSourceDataModel GetTestSourceDataModel(TestGenerationContext testGenerationContext)
    {
        var methodSymbol = testGenerationContext.MethodSymbol;
        var namedTypeSymbol = testGenerationContext.ClassSymbol;
        var classArguments = testGenerationContext.ClassArguments;
        var testArguments = testGenerationContext.TestArguments;
        var testAttribute = testGenerationContext.TestAttribute;
        
        var allAttributes = methodSymbol.GetAttributesIncludingClass(namedTypeSymbol);
        
        return new TestSourceDataModel
        {
            TestId = TestInformationRetriever.GetTestId(testGenerationContext),
            MethodName = methodSymbol.Name,
            FullyQualifiedTypeName = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            MinimalTypeName = namedTypeSymbol.Name,
            RepeatLimit = TestInformationRetriever.GetRepeatCount(allAttributes),
            CurrentRepeatAttempt = testGenerationContext.CurrentRepeatAttempt,
            ClassArguments = classArguments,
            ClassDataInvocations = GenerateInvocations(classArguments, testGenerationContext.HasEnumerableClassMethodData, "class").ToArray(),
            ClassVariables = GetVariables(classArguments, testGenerationContext.HasEnumerableClassMethodData, "class"),
            MethodDataInvocations = GenerateInvocations(testArguments, testGenerationContext.HasEnumerableTestMethodData, "method").ToArray(),
            MethodVariables = GetVariables(testArguments, testGenerationContext.HasEnumerableTestMethodData, "method"),
            IsEnumerableClassArguments = testGenerationContext.HasEnumerableClassMethodData,
            IsEnumerableMethodArguments = testGenerationContext.HasEnumerableTestMethodData,
            MethodArguments = testArguments,
            FilePath = testAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty,
            LineNumber = testAttribute.ConstructorArguments[1].Value as int? ?? 0,
            MethodParameterTypes = [..methodSymbol.Parameters.Select(x => x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))],
            MethodParameterNames = [..methodSymbol.Parameters.Select(x => x.Name)],
            MethodGenericTypeCount = methodSymbol.TypeParameters.Length,
            CustomDisplayName = allAttributes.FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) == WellKnownFullyQualifiedClassNames.DisplayNameAttribute.WithGlobalPrefix)?.ConstructorArguments.First().Value as string,
            HasTimeoutAttribute = allAttributes.Any(x => x.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames.TimeoutAttribute.WithGlobalPrefix) == true),
            TestExecutor = allAttributes.FirstOrDefault(x => x.AttributeClass?.IsOrInherits("global::" + typeof(TestExecutorAttribute).FullName) == true)?.AttributeClass?.TypeArguments.FirstOrDefault()?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            ParallelLimit = allAttributes.FirstOrDefault(x => x.AttributeClass?.IsOrInherits("global::" + typeof(ParallelLimiterAttribute).FullName) == true)?.AttributeClass?.TypeArguments.FirstOrDefault()?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            AttributeTypes = allAttributes.Select(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)).OfType<string>().Distinct().ToArray(), 
            ClassConstructorType = testGenerationContext.ClassConstructorType
        };
    }

    private static string[] GetVariables(Argument[] arguments, bool hasEnumerableData, string prefix)
    {
        if (hasEnumerableData)
        {
            return arguments[0].TupleVariableNames ?? [$"{prefix}Arg0"];
        }

        return arguments.SelectMany((x, i) => x.TupleVariableNames ?? [$"{prefix}Arg{i}"]).ToArray();
    }

    private static IEnumerable<string> GenerateInvocations(Argument[] arguments, bool hasEnumerableData, string prefix)
    {
        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            var invocation = hasEnumerableData ? $"{prefix}Data" : argument.Invocation;
            var variableName = argument.TupleVariableNames != null
                ? $"({string.Join(", ", argument.TupleVariableNames)})"
                : $"{prefix}Arg{index}";
            var varOrType = hasEnumerableData || argument.TupleVariableNames != null ? "var" : argument.Type;

            yield return $"{varOrType} {variableName} = {invocation};";
        }
    }
}