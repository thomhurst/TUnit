using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.AfterTests; 

public class TestSessionAfterHooksTests
{
    // TODO: The "After(TestSession)" hook is currently not being called/source generated
    [After(TestSession)]
    public static async Task AfterTestSession(TestSessionContext context)
    {
        await Task.CompletedTask;
    }

    [AfterEvery(TestSession)]
    public static async Task AfterEveryTestSession(TestSessionContext context)
    {
        var test = context.AllTests.FirstOrDefault(x =>
            x.TestDetails.TestName == nameof(TestSessionAfterTests.PepareForAfterSession));

        if (test == null)
        {
            // This test might not have been executed due to filters, so the below exception would cause problems.
            return;
        }

        await Assert.That(test.ObjectBag["AfterEveryTestSessionHit"]).IsEqualTo(true);
    }
}

public class TestSessionAfterTests
{
    [Test]
    public async Task PepareForAfterSession()
    {
        TestContext.Current?.ObjectBag.Add("AfterEveryTestSessionHit", true);
        await Assert.That(TestContext.Current?.ObjectBag["AfterEveryTestSessionHit"]).IsEqualTo(true);
    }
}
