﻿namespace TUnit.TestProject.BeforeTests;

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
        await FilePolyfill.WriteAllTextAsync($"TestSessionBeforeTests{Guid.NewGuid():N}.txt", $"{context.AllTests.Count()} tests in session");

        var test = context.AllTests.FirstOrDefault(x =>
            x.Metadata.TestDetails.TestName == nameof(TestSessionBeforeTests.EnsureBeforeEveryTestSessionHit));

        if (test != null)
        {
            test.StateBag.Items["BeforeEveryTestSession"] = true;
        }
    }
}

public class TestSessionBeforeTests
{
    [Test]
    public async Task EnsureBeforeEveryTestSessionHit()
    {
        await Assert.That(TestContext.Current?.StateBag.Items["BeforeEveryTestSession"]).IsEquatableOrEqualTo(true);
    }
}
