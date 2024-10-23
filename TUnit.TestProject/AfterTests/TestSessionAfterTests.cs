using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.AfterTests; 

public class TestSessionAfterHooksTests
{
    [After(TestSession)]
    public static async Task AfterTestSession(TestSessionContext context)
    {
        await Task.CompletedTask;
    }

    [AfterEvery(TestSession)]
    public static async Task AfterEveryTestSession(TestSessionContext context)
    {
        await File.WriteAllTextAsync($"TestSessionAfterTests{Guid.NewGuid():N}.txt", $"{context.AllTests.Count()} tests in session");

        var test = context.AllTests.FirstOrDefault(x =>
            x.TestDetails.TestName == nameof(TestSessionAfterTests.PepareForAfterSession));

        if (test == null)
        {
            // This test might not have been executed due to filters, so the below exception would cause problems.
            return;
        }

        await Assert.That(test.ObjectBag["AfterEveryTestSessionHit"]).IsEquatableOrEqualTo(true);
    }
}

public class TestSessionAfterTests
{
    [Test]
    public async Task PepareForAfterSession()
    {
        TestContext.Current?.ObjectBag.Add("AfterEveryTestSessionHit", true);
        await Assert.That(TestContext.Current?.ObjectBag["AfterEveryTestSessionHit"]).IsEquatableOrEqualTo(true);
    }
}
