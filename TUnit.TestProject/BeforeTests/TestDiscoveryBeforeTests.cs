namespace TUnit.TestProject.BeforeTests;

public class TestDiscoveryBeforeTests
{
    // TODO: The "Before(TestDiscovery)" hook is currently not being called/source generated
    [Before(TestDiscovery)]
    public static async Task BeforeTestDiscovery(BeforeTestDiscoveryContext context)
    {
        await Task.CompletedTask;
    }

    [BeforeEvery(TestDiscovery)]
    public static async Task BeforeEveryTestDiscovery(BeforeTestDiscoveryContext context)
    {
        await File.WriteAllTextAsync("TestDiscoveryBeforeTests.txt", $"Blah!");

    }
}
