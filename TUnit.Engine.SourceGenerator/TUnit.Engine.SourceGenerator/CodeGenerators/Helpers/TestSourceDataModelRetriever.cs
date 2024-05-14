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

        var classArguments = ClassArgumentsRetriever.GetClassArguments(namedTypeSymbol);
        var testArgumentsContainer =
            MethodArgumentsRetriever.GetMethodArguments(methodSymbol, namedTypeSymbol, testType);
        var repeatCount =
            TestInformationRetriever.GetRepeatCount(methodSymbol.GetAttributesIncludingClass(namedTypeSymbol));

        var runCount = repeatCount + 1;

        foreach (var testArguments in testArgumentsContainer)
        {
            if (!classArguments.Arguments.Any())
            {
                foreach (var testSourceDataModel in GenerateSingleClassInstance(methodSymbol, namedTypeSymbol, runCount, testAttribute,
                             testArguments))
                {
                    yield return testSourceDataModel;
                }
            }

            foreach (var generateMultipleClassInstance in GenerateMultipleClassInstances(methodSymbol, namedTypeSymbol, runCount, testAttribute,
                         classArguments, testArguments))
            {
                yield return generateMultipleClassInstance;
            }
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateSingleClassInstance(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute,
        ArgumentsContainer testArguments)
    {
        for (var i = 0; i < runCount; i++)
        {
            yield return GetTestSourceDataModel(new TestGenerationContext()
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
        foreach (var classArgument in classArguments.Arguments)
        {
            for (var i = 0; i < runCount; i++)
            {
                yield return GetTestSourceDataModel(new TestGenerationContext
                {
                    MethodSymbol = methodSymbol,
                    ClassSymbol = namedTypeSymbol,
                    ClassArguments = [classArgument],
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
    }

    private static TestSourceDataModel GetTestSourceDataModel(TestGenerationContext testGenerationContext)
    {
        var methodSymbol = testGenerationContext.MethodSymbol;
        var namedTypeSymbol = testGenerationContext.ClassSymbol;
        var classArgument = testGenerationContext.ClassArguments;
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
            Timeout = TestInformationRetriever.GetTimeOut(allAttributes),
            ReturnType = TestInformationRetriever.GetReturnType(methodSymbol),
            Order = TestInformationRetriever.GetOrder(allAttributes),
            RetryCount = TestInformationRetriever.GetRetryCount(allAttributes),
            RepeatIndex = TestInformationRetriever.GetRepeatCount(allAttributes),
            Categories = string.Join(", ", TestInformationRetriever.GetCategories(allAttributes)),
            NotInParallelConstraintKeys = TestInformationRetriever.GetNotInParallelConstraintKeys(allAttributes),
            ClassArguments = classArgument,
            IsEnumerableClassArguments = testGenerationContext.HasEnumerableClassMethodData,
            IsEnumerableMethodArguments = testGenerationContext.HasEnumerableTestMethodData,
            MethodArguments = testArguments,
            FilePath = testAttribute.ConstructorArguments[0].Value!.ToString(),
            LineNumber = (int)testAttribute.ConstructorArguments[1].Value!,
            BeforeEachTestInvocations = BeforeEachTestRetriever.GenerateCode(namedTypeSymbol),
            AfterEachTestInvocations = AfterEachTestRetriever.GenerateCode(namedTypeSymbol),
            CustomProperties = CustomPropertiesRetriever.GetCustomProperties(allAttributes),
            ApplicableTestAttributes = CustomTestAttributeRetriever.GetCustomAttributes(namedTypeSymbol, methodSymbol),
        };
    }
}