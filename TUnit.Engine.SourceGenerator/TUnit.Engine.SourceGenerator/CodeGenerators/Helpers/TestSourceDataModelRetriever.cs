using System.Collections.Generic;
using System.Collections.Immutable;
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

        var classArguments = ClassArgumentsRetriever.GetClassArguments(namedTypeSymbol).ToArray();
        var testArguments = MethodArgumentsRetriever.GetMethodArguments(methodSymbol, namedTypeSymbol, testType).ToArray();
        var repeatCount =
            TestInformationRetriever.GetRepeatCount(methodSymbol.GetAttributesIncludingClass(namedTypeSymbol));

        var runCount = repeatCount + 1;
        
        if (!classArguments.Any())
        {
            return GenerateSingleClassInstance(methodSymbol, namedTypeSymbol, runCount, testAttribute, testArguments);
        }

        return GenerateMultipleClassInstances(methodSymbol, namedTypeSymbol, runCount, testAttribute, classArguments, testArguments);
    }

    private static IEnumerable<TestSourceDataModel> GenerateSingleClassInstance(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute,
        IEnumerable<Argument>[] testArgumentsCollection)
    {
        var methodCount = 0;

        for (var i = 1; i <= runCount; i++)
        {
            if (!testArgumentsCollection.Any())
            {
                yield return GetTestSourceDataModel(methodSymbol, namedTypeSymbol, testAttribute, null, [], 1, ++methodCount);
                continue;
            }

            foreach (var testArguments in testArgumentsCollection)
            {
                yield return GetTestSourceDataModel(methodSymbol, namedTypeSymbol, testAttribute, null, [..testArguments], 1,
                    ++methodCount);
            }
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateMultipleClassInstances(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute, Argument[] classArguments,
        IEnumerable<Argument>[] testArgumentsCollection)
    {
        var classCount = 0;
        foreach (var classArgument in classArguments)
        {
            classCount++;
            var methodCount = 0;

            for (var i = 1; i <= runCount; i++)
            {
                if (!testArgumentsCollection.Any())
                {
                    yield return GetTestSourceDataModel(methodSymbol, namedTypeSymbol, testAttribute, classArgument, [],
                        classCount, ++methodCount);
                    continue;
                }
                
                foreach (var testArguments in testArgumentsCollection)
                {
                    yield return GetTestSourceDataModel(methodSymbol, namedTypeSymbol, testAttribute, classArgument,
                        [..testArguments], classCount, ++methodCount);
                }
            }
        }
    }

    private static TestSourceDataModel GetTestSourceDataModel(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol,
        AttributeData testAttribute,
        Argument? classArgument,
        Argument[] testArguments,
        int currentClassCount,
        int currentMethodCount)
    {
        var allAttributes = methodSymbol.GetAttributesIncludingClass(namedTypeSymbol);
        
        return new TestSourceDataModel
        {
            TestId = TestInformationRetriever.GetTestId(namedTypeSymbol, methodSymbol, testAttribute, testArguments, currentClassCount,
                currentMethodCount),
            MethodName = methodSymbol.Name,
            FullyQualifiedTypeName =
                namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            MinimalTypeName = namedTypeSymbol.Name,
            CurrentClassRepeatCount = currentClassCount,
            CurrentMethodRepeatCount = currentMethodCount,
            Timeout = TestInformationRetriever.GetTimeOut(allAttributes),
            ReturnType = TestInformationRetriever.GetReturnType(methodSymbol),
            Order = TestInformationRetriever.GetOrder(allAttributes),
            RetryCount = TestInformationRetriever.GetRetryCount(allAttributes),
            RepeatCount = TestInformationRetriever.GetRepeatCount(allAttributes),
            Categories = string.Join(", ", TestInformationRetriever.GetCategories(allAttributes)),
            NotInParallelConstraintKeys = TestInformationRetriever.GetNotInParallelConstraintKeys(allAttributes),
            ClassArguments = classArgument == null ? [] : [classArgument], // TODO: Proper array at some point?
            IsEnumerableClassArguments = classArgument?.ArgumentSource == ArgumentSource.EnumerableMethodDataAttribute,
            IsEnumerableMethodArguments = testArguments.FirstOrDefault()?.ArgumentSource == ArgumentSource.EnumerableMethodDataAttribute,
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