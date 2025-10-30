namespace TUnit.TestProject;

public class BasicTestsHooksFromLibrary : TUnit.TestProject.Library.Hooks
{
    [Test]
    public async Task Test()
    {
        await Assert.That(TestContext.Current!.StateBag.Items["BeforeHit"]).IsEquatableOrEqualTo(true);
    }

    [After(Class)]
    public static async Task AfterClass(ClassHookContext context)
    {
        await Assert.That(context.Tests.First().StateBag.Items["AfterHit"]).IsEquatableOrEqualTo(true);
    }
}
