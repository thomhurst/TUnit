namespace TUnit.TestProject.BeforeTests;

public class BeforeEveryClassHooks
{
    [BeforeEvery(Class)]
    public static void BeforeEveryClass(ClassHookContext context)
    {
        foreach (var test in context.Tests)
        {
            test.StateBag.Items["BeforeEveryClassHit"] = true;
        }
    }
}

public class BeforeEveryClassTests
{
    [Test]
    public async Task EnsureBeforeEveryClassHit()
    {
        await Assert.That(TestContext.Current?.StateBag.Items["BeforeEveryClassHit"]).IsEquatableOrEqualTo(true);
    }
}
