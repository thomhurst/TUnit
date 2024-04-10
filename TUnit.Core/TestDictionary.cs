namespace TUnit.Core;

public static class TestDictionary
{
    public static readonly AsyncLocal<TestContext> TestContexts = new();
    private static readonly Dictionary<string, UnInvokedTest> Tests = new();

    public static void AddTest(string testId, UnInvokedTest unInvokedTest)
    {
        Tests[testId] = unInvokedTest;
    }

    internal static UnInvokedTest GetTest(string id)
    {
        return Tests[id] ?? throw new Exception($"Test with ID {id} was not found");
    }
    
    internal static IEnumerable<TestInformation> GetAllTestDetails()
    {
        return Tests.Values
            .Select(x => x.TestContext.TestInformation);
    }
}