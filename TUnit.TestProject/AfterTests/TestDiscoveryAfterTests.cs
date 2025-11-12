namespace TUnit.TestProject.AfterTests;

public class TestDiscoveryAfterHooks
{
    [After(TestDiscovery)]
    public static async Task AfterTestDiscovery(TestDiscoveryContext context)
    {
        await Task.CompletedTask;
    }

    [AfterEvery(TestDiscovery)]
    public static async Task AfterEveryTestDiscovery(TestDiscoveryContext context)
    {
        await File.WriteAllTextAsync($"TestDiscoveryAfterTests{Guid.NewGuid():N}.txt", $"{context.AllTests.Count()} tests found");

        var test = context.AllTests.FirstOrDefault(x =>
            x.Metadata.TestDetails.TestName == nameof(TestDiscoveryAfterTests.EnsureAfterEveryTestDiscoveryHit));

        if (test is not null)
        {
            test.StateBag.Items["AfterEveryTestDiscoveryHit"] = true;
        }
    }
}

public class TestDiscoveryAfterTests
{
    [Test]
    public async Task EnsureAfterEveryTestDiscoveryHit()
    {
        await Assert.That(TestContext.Current?.StateBag.Items["AfterEveryTestDiscoveryHit"]).IsEquatableOrEqualTo(true);
    }
}
