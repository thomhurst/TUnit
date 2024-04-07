namespace TUnit.Core;

public static class TestDictionary
{
    public static readonly AsyncLocal<TestContext> TestContexts = new();
    private static readonly Dictionary<string, Lazy<UnInvokedTest>> Tests = new();

    public static void AddTest(string testId, Func<UnInvokedTest> action)
    {
        Tests[testId] = new Lazy<UnInvokedTest>(action);
    }

    internal static Lazy<UnInvokedTest> GetTest(string id)
    {
        return Tests[id] ?? throw new Exception($"Test with ID {id} was not found");
    }
    
    internal static IEnumerable<TestInformation> GetAllTestDetails()
    {
        // TODO: Not sure I love that we have to invoke the func
        return Tests.Values
            .Select(x => x.Value)
            .Select(x => x.TestContext.TestInformation);
    }
}