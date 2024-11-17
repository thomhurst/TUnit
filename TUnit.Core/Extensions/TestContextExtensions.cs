﻿using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Extensions;

public static class TestContextExtensions
{
    public static TestContext[] GetTests(this TestContext context, string testName)
    {
        return GetTests(context, testName, []);
    }
    
    public static TestContext[] GetTests(this TestContext context, string testName, Type[] parameterTypes)
    {
        var tests = context.GetService<ITestFinder>().GetTestsByNameAndParameters(
            testName: testName, 
            methodParameterTypes: parameterTypes, 
            classType: context.TestDetails.ClassType, 
            classParameterTypes: context.TestDetails.TestClassParameterTypes);

        if (tests.Any(x => x.TestTask?.IsCompleted is not true))
        {
            throw new Exception("Cannot get unfinished tests - Did you mean to add a [DependsOn] attribute?");
        }
        
        return tests;
    }
    
    public static string GetClassTypeName(this TestContext testContext)
    {
        var testDetails = testContext.TestDetails;
        
        var classTypeName = testDetails.ClassType.FullName?
                                .Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                .LastOrDefault()
                            ?? testDetails.ClassType.Name;
        
        if (testDetails.TestClassArguments.Length == 0)
        {
            return classTypeName;
        }
        
        return
            $"{classTypeName}({string.Join(", ", testDetails.TestClassArguments.Select(x => ArgumentFormatter.GetConstantValue(testContext, x)))})";
    }
    
    public static string GetTestDisplayName(this TestContext testContext)
    {
        var testDetails = testContext.TestDetails;

        if (!string.IsNullOrWhiteSpace(testDetails.DisplayName))
        {
            return testDetails.DisplayName;
        }
        
        if (testDetails.TestMethodArguments.Length == 0)
        {
            return testDetails.TestName;
        }
        
        return
            $"{testDetails.TestName}({string.Join(", ", testDetails.TestMethodArguments.Select(x => ArgumentFormatter.GetConstantValue(testContext, x)))})";
    }
    
    internal static IEnumerable<ITestRegisteredEventReceiver> GetTestRegisteredEventsObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ITestRegisteredEventReceiver>();

    internal static IEnumerable<ITestStartEventReceiver> GetTestStartEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ITestStartEventReceiver>();
    
    internal static IEnumerable<ITestRetryEventReceiver> GetTestRetryEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ITestRetryEventReceiver>();
    
    internal static IEnumerable<ITestEndEventReceiver> GetTestEndEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ITestEndEventReceiver>();
    
    internal static IEnumerable<ITestSkippedEventReceiver> GetTestSkippedEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ITestSkippedEventReceiver>();
    
    internal static IEnumerable<ILastTestInClassEventReceiver> GetLastTestInClassEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ILastTestInClassEventReceiver>();
    
    internal static IEnumerable<ILastTestInAssemblyEventReceiver> GetLastTestInAssemblyEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ILastTestInAssemblyEventReceiver>();
    
    internal static IEnumerable<ILastTestInTestSessionEventReceiver> GetLastTestInTestSessionEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ILastTestInTestSessionEventReceiver>();

    private static IEnumerable<object?> GetPossibleEventObjects(this TestContext context)
    {
        IEnumerable<object?> rawObjects =
        [
            context.Events,
            ..context.TestDetails.DataAttributes,
            ..context.TestDetails.Attributes,
            context.TestDetails.ClassInstance,
            context.InternalDiscoveredTest.ClassConstructor,
            ..context.TestDetails.TestClassArguments,
            ..context.TestDetails.TestMethodArguments,
            ..context.TestDetails.TestClassInjectedPropertyArguments
        ];

        return rawObjects.OfType<IEventReceiver>().OrderBy(x => x.Order);
    }
}