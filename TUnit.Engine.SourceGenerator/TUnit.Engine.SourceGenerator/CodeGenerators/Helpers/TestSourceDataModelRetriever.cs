using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
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

        if (!classArguments.Any())
        {
            return GenerateSingleClassInstance(methodSymbol, namedTypeSymbol, testAttribute, testArguments);
        }

        return GenerateMultipleClassInstances(methodSymbol, namedTypeSymbol, testAttribute, classArguments, testArguments);
    }

    private static IEnumerable<TestSourceDataModel> GenerateSingleClassInstance(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, AttributeData testAttribute, IEnumerable<Argument>[] testArguments)
    {
        if (!testArguments.Any())
        {
            yield return GetTestSourceDataModel(methodSymbol, namedTypeSymbol, testAttribute, null, [], 1, 1);
            yield break;
        }

        var methodCount = 0;
        foreach (var testArgument in testArguments)
        {
            yield return GetTestSourceDataModel(methodSymbol, namedTypeSymbol, testAttribute, null, testArgument, 1, ++methodCount);
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateMultipleClassInstances(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, AttributeData testAttribute, Argument[] classArguments,
        IEnumerable<Argument>[] testArguments)
    {
        var classCount = 0;
        foreach (var classArgument in classArguments)
        {
            if (!testArguments.Any())
            {
                yield return GetTestSourceDataModel(methodSymbol, namedTypeSymbol, testAttribute, classArgument, [], ++classCount, 1);
                yield break;
            }

            var methodCount = 0;
            foreach (var testArgument in testArguments)
            {
                yield return GetTestSourceDataModel(methodSymbol, namedTypeSymbol, testAttribute, classArgument, testArgument, ++classCount, ++methodCount);
            }
        }
    }

    private static TestSourceDataModel GetTestSourceDataModel(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol,
        AttributeData testAttribute,
        Argument? classArgument,
        IEnumerable<Argument> testArguments,
        int currentClassCount,
        int currentMethodCount)
    {
        var allAttributes = methodSymbol.GetAttributesIncludingClass(namedTypeSymbol);

        return new TestSourceDataModel
        {
            TestId = TestInformationRetriever.GetTestId(namedTypeSymbol, methodSymbol, currentClassCount, currentMethodCount),
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
            MethodArguments = testArguments.ToArray(),
        };
    }
}