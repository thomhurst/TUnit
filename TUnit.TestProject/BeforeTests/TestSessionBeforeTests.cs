using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.BeforeTests;

public class TestSessionBeforeHooks
{
    // TODO: The "Before(TestSession)" hook is currently not being called/source generated
    [Before(TestSession)]
    public static async Task BeforeTestSession(TestSessionContext context)
    {
        await Task.CompletedTask;
    }

    [BeforeEvery(TestSession)]
    public static void BeforeEveryTestSession(TestSessionContext context)
    {
        var firstTest = context.AllTests.First();
        context.OutputWriter.WriteLine($"BeforeEveryTestSession: {firstTest.TestDetails.TestName}");

        foreach (var test in context.AllTests)
        {
            test.ObjectBag.Add("BeforeEveryTestSession", true);
        }
    }
}

public class TestSessionBeforeTests
{
    [Test]
    public async Task EnsureBeforeEveryTestSessionHit()
    {
        await Assert.That(TestContext.Current?.ObjectBag["BeforeEveryTestSession"]).IsEqualTo(true);
    }
}