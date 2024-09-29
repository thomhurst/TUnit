namespace TUnit.TestProject.BeforeTests;

public class TestDiscoveryBeforeTests
{
    // TODO: The "Before(TestDiscovery)" hook is currently not being called/source generated
    [Before(TestDiscovery)]
    public static async Task BeforeTestDiscovery(BeforeTestDiscoveryContext context)
    {
        await Task.CompletedTask;
    }

    // TODO: What could make sense to test here? We dont have any kind of informations yet.
    [BeforeEvery(TestDiscovery)]
    public static async Task BeforeEveryTestDiscovery(BeforeTestDiscoveryContext context)
    {
        await Task.CompletedTask;
    }
}
