namespace TUnit.TestProject.AfterTests;

public class AfterEveryAssemblyHooks
{
    [AfterEvery(Assembly)]
    public static async Task AfterEveryAssembly(AssemblyHookContext context)
    {
        foreach (var test in context.AllTests)
        {
            if (test.Metadata.TestDetails.TestName == nameof(AfterEveryAssemblyTests.EnsureAfterEveryAssemblyRuns))
            {
                await File.WriteAllTextAsync($"AfterEveryAssemblyTests{Guid.NewGuid():N}.txt", "AfterEvery(Assembly) executed");
            }
        }
    }
}

public class AfterEveryAssemblyTests
{
    [Test]
    public async Task EnsureAfterEveryAssemblyRuns()
    {
        await Task.CompletedTask;
    }
}
