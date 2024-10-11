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
        var tests = TestDictionary.GetTestsByNameAndParameters(testName, parameterTypes, context.TestDetails.ClassType, context.TestDetails.TestClassParameterTypes)
            .Select(x => x.TestContext)
            .ToArray();

        if (tests.Any(x => !x.TestTask.IsCompleted))
        {
            throw new Exception("Cannot get unfinished tests - Did you mean to add a [DependsOn] attribute?");
        }
        
        return tests;
    }
    
    internal static IEnumerable<ITestRegisteredEvents> GetTestRegisteredEventsObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ITestRegisteredEvents>();

    internal static IEnumerable<ITestStartEvent> GetTestStartEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ITestStartEvent>();
    
    internal static IEnumerable<ITestEndEvent> GetTestEndEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ITestEndEvent>();
    
    internal static IEnumerable<ILastTestInClassEvent> GetLastTestInClassEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ILastTestInClassEvent>();
    
    internal static IEnumerable<ILastTestInAssemblyEvent> GetLastTestInAssemblyEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ILastTestInAssemblyEvent>();
    
    internal static IEnumerable<ILastTestInTestSessionEvent> GetLastTestInTestSessionEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context).OfType<ILastTestInTestSessionEvent>();

    private static IEnumerable<object?> GetPossibleEventObjects(this TestContext context)
    {
        return
        [
            ..context.TestDetails.DataAttributes,
            ..context.TestDetails.Attributes,
            context.TestDetails.ClassInstance,
            context.InternalDiscoveredTest.ClassConstructor
        ];
    }
}