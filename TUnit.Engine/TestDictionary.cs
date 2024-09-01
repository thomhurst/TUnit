using TUnit.Core;

namespace TUnit.Engine;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class TestDictionary
{
    private static readonly List<DiscoveredTest> Tests = new();
    private static readonly Dictionary<string, FailedInitializationTest> FailedInitializationTests = new();

    internal static void AddTest(DiscoveredTest discoveredTest)
    {
        Tests.Add(discoveredTest);
    }

    public static void RegisterFailedTest(string testId, FailedInitializationTest failedInitializationTest)
    {
        FailedInitializationTests[testId] = failedInitializationTest;
    }
    
    internal static IEnumerable<DiscoveredTest> GetAllTests()
    {
        return Tests.AsParallel().ToList();
    }

    internal static DiscoveredTest[] GetTestsByNameAndParameters(string testName, IEnumerable<Type> methodParameterTypes, Type classType, IEnumerable<Type> classParameterTypes)
    {
        var testsWithoutMethodParameterTypesMatching = Tests.Where(x =>
            x.TestContext.TestDetails.TestName == testName &&
            x.TestContext.TestDetails.ClassType == classType &&
            x.TestContext.TestDetails.TestClassParameterTypes.SequenceEqual(classParameterTypes))
            .ToArray();

        if (testsWithoutMethodParameterTypesMatching.GroupBy(x => string.Join(", ", x.TestContext.TestDetails.TestMethodParameterTypes.Select(t => t.FullName)))
                .Count() > 1)
        {
            return testsWithoutMethodParameterTypesMatching.Where(x =>
                    x.TestContext.TestDetails.TestMethodParameterTypes.SequenceEqual(methodParameterTypes)).ToArray();
        }
        
        return testsWithoutMethodParameterTypesMatching;
    }
    
    internal static FailedInitializationTest[] GetFailedToInitializeTests()
    {
        return FailedInitializationTests.Values.ToArray();
    }
}