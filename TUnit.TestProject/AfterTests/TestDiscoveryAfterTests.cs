using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.AfterTests;

public class TestDiscoveryAfterHooks
{
    // TODO: The "After(TestDiscovery)" hook is currently not being called/source generated
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
            x.TestDetails.TestName == nameof(TestDiscoveryAfterTests.EnsureAfterEveryTestDiscovoryHit));

        if (test == null)
        {
            // This test might not have been executed due to filters, so the below exception would cause problems.
            return;
        }

        test.ObjectBag.Add("AfterEveryTestDiscoveryHit", true);
    }
}

public class TestDiscoveryAfterTests
{
    [Test]
    public async Task EnsureAfterEveryTestDiscovoryHit()
    {
        await Assert.That(TestContext.Current?.ObjectBag["AfterEveryTestDiscoveryHit"]).IsEquatableOrEqualTo(true);
    }
}
