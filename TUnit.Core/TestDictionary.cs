namespace TUnit.Core;

public record UninvokedTest
{
    public required string Id { get; init; }
    public required TestContext TestContext { get; init; }
    
    public required List<Func<Task>> OneTimeSetUps { get; init; }
    public required List<Func<Task>> SetUps { get; init; }
    
    public required Func<Task> TestBody { get; init; }
    
    public required List<Func<Task>> CleanUps { get; init; }
    public required List<Func<Task>> OneTimeCleanUps { get; init; }
}

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