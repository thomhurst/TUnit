using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.CodeGenerators.Writers;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

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
                    RepeatIndex = i,
                    TestAttribute = testAttribute,
                    HasEnumerableTestMethodData = testArguments.IsEnumerableData,
                    HasEnumerableClassMethodData = false,
                    ClassDataAttributeIndex = null,
                    TestDataAttributeIndex = testArguments.DataAttributeIndex
                });
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateMultipleClassInstances(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute, ArgumentsContainer classArguments,
        ArgumentsContainer testArgumentsCollection)
    {
        for (var i = 0; i < runCount; i++)
        {
            yield return GetTestSourceDataModel(new TestGenerationContext
            {
                MethodSymbol = methodSymbol,
                ClassSymbol = namedTypeSymbol,
                ClassArguments = classArguments.Arguments,
                TestArguments = testArgumentsCollection.Arguments,
                ClassDataSourceAttribute = classArguments.DataAttribute,
                TestDataAttribute = testArgumentsCollection.DataAttribute,
                RepeatIndex = i,
                TestAttribute = testAttribute,
                HasEnumerableTestMethodData = testArgumentsCollection.IsEnumerableData,
                HasEnumerableClassMethodData = classArguments.IsEnumerableData,
                TestDataAttributeIndex = testArgumentsCollection.DataAttributeIndex,
                ClassDataAttributeIndex = classArguments.DataAttributeIndex
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
        var currentClassCount = 0;
        
        var allAttributes = methodSymbol.GetAttributesIncludingClass(namedTypeSymbol);
        
        return new TestSourceDataModel
        {
            TestId = TestInformationRetriever.GetTestId(testGenerationContext),
            MethodName = methodSymbol.Name,
            FullyQualifiedTypeName =
                namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            MinimalTypeName = namedTypeSymbol.Name,
            CurrentClassRepeatCount = currentClassCount,
            CurrentMethodRepeatCount = testGenerationContext.RepeatIndex,
            ReturnType = TestInformationRetriever.GetReturnType(methodSymbol),
            RepeatCount = TestInformationRetriever.GetRepeatCount(allAttributes),
            RepeatIndex = testGenerationContext.RepeatIndex,
            ClassArguments = classArguments,
            IsEnumerableClassArguments = testGenerationContext.HasEnumerableClassMethodData,
            IsEnumerableMethodArguments = testGenerationContext.HasEnumerableTestMethodData,
            MethodArguments = testArguments,
            FilePath = testAttribute.ConstructorArguments[0].Value!.ToString(),
            LineNumber = (int)testAttribute.ConstructorArguments[1].Value!,
            BeforeEachTestInvocations = BeforeEachTestRetriever.GenerateCode(namedTypeSymbol),
            AfterEachTestInvocations = AfterEachTestRetriever.GenerateCode(namedTypeSymbol),
            ApplicableTestAttributes = CustomTestAttributeRetriever.GetCustomAttributes(allAttributes, namedTypeSymbol),
            MethodParameterTypes = [..methodSymbol.Parameters.Select(x => x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))],
            ClassParameterTypes = [..namedTypeSymbol.Constructors.First().Parameters.Select(x => x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))],
            MethodGenericTypeCount = methodSymbol.TypeParameters.Length,
            CustomDisplayName = allAttributes.FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) == WellKnownFullyQualifiedClassNames.DisplayNameAttribute.WithGlobalPrefix)?.ConstructorArguments.First().Value as string
        };
    }
}