using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class BasicTestsHooksFromLibrary : TUnit.TestProject.Library.Hooks
{
    [Test]
    public async Task Test()
    {
        await Assert.That(TestContext.Current!.ObjectBag["BeforeHit"]).IsEquatableOrEqualTo(true);
    }

    [After(Class)]
    public static async Task AfterClass(ClassHookContext context)
    {
        await Assert.That(context.Tests.First().ObjectBag["AfterHit"]).IsEquatableOrEqualTo(true);
    }
}