namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class TestDictionary
{
    private static readonly Dictionary<string, UnInvokedTest> Tests = new();
    private static readonly Dictionary<string, FailedInitializationTest> FailedInitializationTests = new();

    public static void AddTest(string testId, UnInvokedTest unInvokedTest)
    {
        Tests[testId] = unInvokedTest;
    }

    public static void RegisterFailedTest(string testId, FailedInitializationTest failedInitializationTest)
    {
        FailedInitializationTests[testId] = failedInitializationTest;
    }
    
    internal static FailedInitializationTest GetFailedInitializationTest(string id)
    {
        return FailedInitializationTests[id] ?? throw new Exception($"Test with ID {id} was not found");
    }
    
    internal static IEnumerable<UnInvokedTest> GetAllTests()
    {
        return Tests.Values;
    }

    internal static IEnumerable<TestContext> GetTestsByNameAndParameters(string testName, IEnumerable<Type> methodParameterTypes, Type classType, IEnumerable<Type> classParameterTypes)
    {
        return Tests.Values.Where(x =>
                x.TestContext.TestInformation.TestName == testName &&
                x.TestContext.TestInformation.ClassType == classType &&
                x.TestContext.TestInformation.TestMethodParameterTypes.SequenceEqual(methodParameterTypes) &&
               x.TestContext.TestInformation.TestClassParameterTypes.SequenceEqual(classParameterTypes))
            .Select(x => x.TestContext);
    }
    
    internal static IEnumerable<FailedInitializationTest> GetFailedToInitializeTests()
    {
        return FailedInitializationTests.Values;
    }
}