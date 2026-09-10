namespace TUnit.TestProject.BeforeTests;

public class BeforeEveryAssemblyHooks
{
    [BeforeEvery(Assembly)]
    public static void BeforeEveryAssembly(AssemblyHookContext context)
    {
        foreach (var test in context.AllTests)
        {
            if (test.Metadata.TestDetails.TestName == nameof(BeforeEveryAssemblyTests.EnsureBeforeEveryAssemblyHit))
            {
                test.StateBag.Items["BeforeEveryAssemblyHit"] = true;
            }
        }
    }
}

public class BeforeEveryAssemblyTests
{
    [Test]
    public async Task EnsureBeforeEveryAssemblyHit()
    {
        await Assert.That(TestContext.Current?.StateBag.Items["BeforeEveryAssemblyHit"]).IsEquatableOrEqualTo(true);
    }
}
