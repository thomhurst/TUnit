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
    public static void AfterEveryTestDiscovery(TestDiscoveryContext context)
    {
        var firstTest = context.AllTests.First();
        context.OutputWriter.WriteLine($"AfterEveryTestDiscovery: {firstTest.TestDetails.TestName}");

        foreach (var test in context.AllTests)
        {
            test.ObjectBag.Add("AfterEveryTestDiscoveryHit", true);
        }
    }
}

public class TestDiscoveryAfterTests
{
    [Test]
    public async Task EnsureAfterEveryTestDiscovoryHit()
    {
        await Assert.That(TestContext.Current?.ObjectBag["AfterEveryTestDiscoveryHit"]).IsEqualTo(true);
    }
}
