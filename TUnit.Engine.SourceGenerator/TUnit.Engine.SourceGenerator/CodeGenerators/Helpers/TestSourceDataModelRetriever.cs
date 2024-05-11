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
        var testArguments = MethodArgumentsRetriever.GetMethodArguments(methodSymbol, namedTypeSymbol, testType).ToArray();
        var repeatCount =
            TestInformationRetriever.GetRepeatCount(methodSymbol.GetAttributesIncludingClass(namedTypeSymbol));

        var runCount = repeatCount + 1;
        
        if (!classArguments.Arguments.Any())
        {
            return GenerateSingleClassInstance(methodSymbol, namedTypeSymbol, runCount, testAttribute, testArguments);
        }

        return GenerateMultipleClassInstances(methodSymbol, namedTypeSymbol, runCount, testAttribute, classArguments, testArguments);
    }

    private static IEnumerable<TestSourceDataModel> GenerateSingleClassInstance(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute,
        ArgumentsContainer[] testArgumentsCollection)
    {
        var methodCount = 0;

        for (var i = 1; i <= runCount; i++)
        {
            if (!testArgumentsCollection.Any())
            {
                yield return GetTestSourceDataModel(new TestGenerationContext()
                {
                    MethodSymbol = methodSymbol,
                    ClassSymbol = namedTypeSymbol,
                    ClassArguments = [],
                    TestArguments = [],
                    ClassDataAttribute = null,
                    TestDataAttribute = null,
                    RepeatCount = ++methodCount,
                    TestAttribute = testAttribute,
                    EnumerableTestMethodDataCurrentCount = 0,
                    EnumerableClassMethodDataCurrentCount = null
                });
                continue;
            }

            foreach (var testArguments in testArgumentsCollection)
            {
                yield return GetTestSourceDataModel(new TestGenerationContext()
                {
                    MethodSymbol = methodSymbol,
                    ClassSymbol = namedTypeSymbol,
                    ClassArguments = [],
                    TestArguments = testArguments.Arguments,
                    ClassDataAttribute = null,
                    TestDataAttribute = testArguments.DataAttribute,
                    RepeatCount = ++methodCount,
                    TestAttribute = testAttribute,
                    EnumerableTestMethodDataCurrentCount = 0,
                    EnumerableClassMethodDataCurrentCount = null
                });
            }
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateMultipleClassInstances(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute, ArgumentsContainer classArguments,
        ArgumentsContainer[] testArgumentsCollection)
    {
        var classCount = 0;
        foreach (var classArgument in classArguments.Arguments)
        {
            classCount++;
            var methodCount = 0;

            for (var i = 1; i <= runCount; i++)
            {
                if (!testArgumentsCollection.Any())
                {
                    yield return GetTestSourceDataModel(new TestGenerationContext()
                    {
                        MethodSymbol = methodSymbol,
                        ClassSymbol = namedTypeSymbol,
                        ClassArguments = classArguments.Arguments,
                        TestArguments = [],
                        ClassDataAttribute = classArguments.DataAttribute,
                        TestDataAttribute = null,
                        RepeatCount = ++methodCount,
                        TestAttribute = testAttribute,
                        EnumerableTestMethodDataCurrentCount = 0,
                        EnumerableClassMethodDataCurrentCount = classCount
                    });
                    continue;
                }
                
                foreach (var testArguments in testArgumentsCollection)
                {
                    yield return GetTestSourceDataModel(new TestGenerationContext()
                    {
                        MethodSymbol = methodSymbol,
                        ClassSymbol = namedTypeSymbol,
                        ClassArguments = classArguments.Arguments,
                        TestArguments = testArguments.Arguments,
                        ClassDataAttribute = classArguments.DataAttribute,
                        TestDataAttribute = testArguments.DataAttribute,
                        RepeatCount = ++methodCount,
                        TestAttribute = testAttribute,
                        EnumerableTestMethodDataCurrentCount = 0,
                        EnumerableClassMethodDataCurrentCount = null
                    });
                }
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
        var currentMethodCount = testGenerationContext.EnumerableTestMethodDataCurrentCount;
        
        var allAttributes = methodSymbol.GetAttributesIncludingClass(namedTypeSymbol);
        
        return new TestSourceDataModel
        {
            TestId = TestInformationRetriever.GetTestId(testGenerationContext),
            MethodName = methodSymbol.Name,
            FullyQualifiedTypeName =
                namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            MinimalTypeName = namedTypeSymbol.Name,
            CurrentClassRepeatCount = currentClassCount,
            CurrentMethodRepeatCount = testGenerationContext.RepeatCount,
            Timeout = TestInformationRetriever.GetTimeOut(allAttributes),
            ReturnType = TestInformationRetriever.GetReturnType(methodSymbol),
            Order = TestInformationRetriever.GetOrder(allAttributes),
            RetryCount = TestInformationRetriever.GetRetryCount(allAttributes),
            RepeatCount = TestInformationRetriever.GetRepeatCount(allAttributes),
            Categories = string.Join(", ", TestInformationRetriever.GetCategories(allAttributes)),
            NotInParallelConstraintKeys = TestInformationRetriever.GetNotInParallelConstraintKeys(allAttributes),
            ClassArguments = classArgument,
            IsEnumerableClassArguments = testGenerationContext.EnumerableClassMethodDataCurrentCount.HasValue,
            IsEnumerableMethodArguments = testGenerationContext.EnumerableTestMethodDataCurrentCount.HasValue,
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