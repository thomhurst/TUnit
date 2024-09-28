namespace TUnit.TestProject.BeforeTests;

public class TestDiscoveryBeforeTests
{
    [Before(TestDiscovery)]
    public static async Task BeforeTestDiscovery(BeforeTestDiscoveryContext context)
    {
        await Task.CompletedTask;
    }

    [BeforeEvery(TestDiscovery)]
    public static async Task BeforeEveryTestDiscovery(BeforeTestDiscoveryContext context)
    {
        await Task.CompletedTask;
    }
}
