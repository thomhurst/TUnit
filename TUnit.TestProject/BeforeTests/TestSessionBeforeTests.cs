using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.BeforeTests;

public class TestSessionBeforeHooks
{
    [Before(TestSession)]
    public static async Task BeforeTestSession(TestSessionContext context)
    {
        await Task.CompletedTask;
    }

    [BeforeEvery(TestSession)]
    public static async Task BeforeEveryTestSession(TestSessionContext context)
    {
        await File.WriteAllTextAsync($"TestSessionBeforeTests{Guid.NewGuid():N}.txt", $"{context.AllTests.Count()} tests in session");

        var test = context.AllTests.FirstOrDefault(x =>
            x.TestDetails.TestName == nameof(TestSessionBeforeTests.EnsureBeforeEveryTestSessionHit));

        if (test == null)
        {
            // This test might not have been executed due to filters, so the below exception would cause problems.
            return;
        }

        test.ObjectBag.Add("BeforeEveryTestSession", true);
    }
}

public class TestSessionBeforeTests
{
    [Test]
    public async Task EnsureBeforeEveryTestSessionHit()
    {
        await Assert.That(TestContext.Current?.ObjectBag["BeforeEveryTestSession"]).IsEquatableOrEqualTo(true);
    }
}