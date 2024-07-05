using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TestSourceDataModelRetriever
{
    public static IEnumerable<TestSourceDataModel> ParseTestDatas(this IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol,
        TestType testType)
    {
        var testAttribute = methodSymbol.GetRequiredTestAttribute();

        if (testType is TestType.Unknown)
        {
            testType = testAttribute.GetTestType();
        }

        var classArgumentsContainers = ClassArgumentsRetriever.GetClassArguments(namedTypeSymbol).ToList();
        var testArgumentsContainers =
            MethodArgumentsRetriever.GetMethodArguments(methodSymbol, namedTypeSymbol, testType);
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
        if (!classArguments.Arguments.Any())
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
                    SharedClassDataSourceKeys = testArguments.Arguments.OfType<KeyedSharedArgument>().Select(x => new SharedInstanceKey(x.Key, x.Type)).ToArray(),
                    InjectedGlobalClassDataSourceTypes = testArguments.Arguments.OfType<GloballySharedArgument>().Select(x => x.Type).ToArray(),
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
                SharedClassDataSourceKeys = classArguments.Arguments.OfType<KeyedSharedArgument>().Concat(testArguments.Arguments.OfType<KeyedSharedArgument>()).Select(x => new SharedInstanceKey(x.Key, x.Type)).ToArray(),
                InjectedGlobalClassDataSourceTypes = classArguments.Arguments.OfType<GloballySharedArgument>().Concat(testArguments.Arguments.OfType<GloballySharedArgument>()).Select(x => x.Type).ToArray(),
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
            ReturnType = TestInformationRetriever.GetReturnType(methodSymbol),
            RepeatLimit = TestInformationRetriever.GetRepeatCount(allAttributes),
            CurrentRepeatAttempt = testGenerationContext.CurrentRepeatAttempt,
            ClassArguments = classArguments,
            IsEnumerableClassArguments = testGenerationContext.HasEnumerableClassMethodData,
            IsEnumerableMethodArguments = testGenerationContext.HasEnumerableTestMethodData,
            MethodArguments = testArguments,
            FilePath = testAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty,
            LineNumber = testAttribute.ConstructorArguments[1].Value as int? ?? 0,
            BeforeEachTestInvocations = BeforeEachTestRetriever.GenerateCode(namedTypeSymbol),
            AfterEachTestInvocations = AfterEachTestRetriever.GenerateCode(namedTypeSymbol),
            ApplicableTestAttributes = CustomTestAttributeRetriever.GetCustomAttributes(allAttributes),
            MethodParameterTypes = [..methodSymbol.Parameters.Select(x => x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))],
            ClassParameterTypes = [..namedTypeSymbol.Constructors.First().Parameters.Select(x => x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))],
            MethodGenericTypeCount = methodSymbol.TypeParameters.Length,
            CustomDisplayName = allAttributes.FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) == WellKnownFullyQualifiedClassNames.DisplayNameAttribute.WithGlobalPrefix)?.ConstructorArguments.First().Value as string,
            SharedClassDataSourceKeys = testGenerationContext.SharedClassDataSourceKeys,
            InjectedGlobalClassDataSourceTypes = testGenerationContext.InjectedGlobalClassDataSourceTypes,
        };
    }
}