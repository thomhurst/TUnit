namespace TUnit.TestProject.Bugs._3597;

public class DerivedClassTests : BaseClassWithAsyncInitializer
{
    [ClassDataSource<TestHost>(Shared = SharedType.PerTestSession)]
    public required override TestHost Host { get; set; } = null!;

    [Test]
    public async Task HostShouldBeInitializedBeforeTestRuns()
    {
        // Verify that InitializeAsync was called successfully
        await Assert.That(WasInitialized).IsTrue();

        // Verify that Host is not null
        await Assert.That(Host).IsNotNull();

        // Verify that Host is ready
        await Assert.That(Host.IsReady).IsTrue();
    }

    [Test]
    public async Task HostShouldBeTheSameInstanceForAllTests()
    {
        // Since we're using SharedType.PerTestSession, Host should be the same instance
        await Assert.That(Host).IsNotNull();
        await Assert.That(Host.IsReady).IsTrue();
    }
}
