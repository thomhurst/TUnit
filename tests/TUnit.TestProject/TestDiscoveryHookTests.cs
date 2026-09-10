using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class TestDiscoveryHookTests
{
    public static int CustomNamedBeforeDiscoveryCalls;
    public static int CustomNamedAfterDiscoveryCalls;

    [BeforeEvery(TestDiscovery, Order = 5)]
    public static void BeforeDiscovery()
    {
    }

    [AfterEvery(TestDiscovery)]
    public static void AfterDiscovery()
    {
    }

    // Regression: method name without "Before"/"After" must still resolve correct
    // context type from the [Before]/[After] attribute kind, not from the method name.
    [Before(TestDiscovery)]
    public static void CustomNamedDiscoveryHook(BeforeTestDiscoveryContext context)
    {
        System.Threading.Interlocked.Increment(ref CustomNamedBeforeDiscoveryCalls);
    }

    [After(TestDiscovery)]
    public static void AnotherCustomNamedDiscoveryHook(TestDiscoveryContext context)
    {
        System.Threading.Interlocked.Increment(ref CustomNamedAfterDiscoveryCalls);
    }

    [Test]
    public async Task CustomNamedDiscoveryHooksWereRegisteredAndRan()
    {
        await Assert.That(CustomNamedBeforeDiscoveryCalls).IsGreaterThan(0);
    }
}
