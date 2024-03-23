namespace TUnit.Core;

public static class TestDictionary
{
    public static readonly AsyncLocal<TestContext> TestContexts = new();
    private static readonly Dictionary<string, Func<Task>> Tests = new();

    public static void AddTest(string testId, Func<Task> action)
    {
        var count = 1;

        while (Tests.ContainsKey($"{testId} {count}"))
        {
            count++;
        }
        
        Tests[$"{testId} {count}"] = action;
    }

    public static Func<Task> GetTest(string id)
    {
        return Tests[id];
    }
}