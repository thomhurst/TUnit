using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public static class TestDictionary
{
    public static readonly AsyncLocal<TestContext> TestContexts = new();
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

    internal static bool TryGetTest(string id, [NotNullWhen(true)] out UnInvokedTest? unInvokedTest)
    {
        return Tests.TryGetValue(id, out unInvokedTest);
    }
    
    internal static FailedInitializationTest GetFailedInitializationTest(string id)
    {
        return FailedInitializationTests[id] ?? throw new Exception($"Test with ID {id} was not found");
    }
    
    internal static IEnumerable<TestInformation> GetAllTestDetails()
    {
        return Tests.Values
            .Select(x => x.TestContext.TestInformation);
    }
    
    internal static IEnumerable<FailedInitializationTest> GetFailedToInitializeTests()
    {
        return FailedInitializationTests.Values;
    }
}