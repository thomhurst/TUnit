using TUnit.Core;

namespace TUnit.Engine.Extensions;

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
}