namespace TUnit.TestProject.AfterTests;

public class AfterEveryClassHooks
{
    [AfterEvery(Class)]
    public static async Task AfterEveryClass(ClassHookContext context)
    {
        foreach (var test in context.Tests)
        {
            if (test.Metadata.TestDetails.TestName == nameof(AfterEveryClassTests.EnsureAfterEveryClassRuns))
            {
                await File.WriteAllTextAsync($"AfterEveryClassTests{Guid.NewGuid():N}.txt", "AfterEvery(Class) executed");
            }
        }
    }
}

public class AfterEveryClassTests
{
    [Test]
    public async Task EnsureAfterEveryClassRuns()
    {
        await Task.CompletedTask;
    }
}
