using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4715;

/// <summary>
/// Reproduction for issue #4715: Test runner stalls when IAsyncInitializer.InitializeAsync()
/// hangs for a long time before eventually timing out.
/// This simulates a Docker container or web server that takes too long to start.
/// </summary>

public class SlowFailingAppHost4715 : IAsyncInitializer
{
    public async Task InitializeAsync()
    {
        // Simulate a slow initialization that eventually times out
        // In the real scenario, this might be waiting for a Docker container to start
        await Task.Delay(TimeSpan.FromSeconds(30));
        throw new TimeoutException("The operation has timed out after waiting for container.");
    }
}

public class SlowWebApplicationFactory4715 : IAsyncInitializer
{
    [ClassDataSource<SlowFailingAppHost4715>(Shared = SharedType.PerTestSession)]
    public required SlowFailingAppHost4715 AppHost { get; init; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// This test should fail within a reasonable time, not stall indefinitely.
/// The 30-second delay in InitializeAsync simulates a slow Docker container startup.
/// </summary>
[EngineTest(ExpectedResult.Failure)]
public class PropertyInjectionSlowInitFailureTests
{
    [ClassDataSource<SlowWebApplicationFactory4715>(Shared = SharedType.PerTestSession)]
    public required SlowWebApplicationFactory4715 Factory { get; init; }

    [Test]
    public void Test_Should_Eventually_Fail()
    {
        throw new InvalidOperationException("This test should not have executed");
    }
}
