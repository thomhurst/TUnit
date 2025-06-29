namespace TUnit.TestProject;

public class TestDiscoveryHookTests
{
    [BeforeEvery(TestDiscovery, Order = 5)]
    public static void BeforeDiscovery()
    {
    }

    [AfterEvery(TestDiscovery)]
    public static void AfterDiscovery()
    {
    }
}
