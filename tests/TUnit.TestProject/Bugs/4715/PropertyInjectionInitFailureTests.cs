using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4715;

/// <summary>
/// Reproduction for issue #4715: Test runner stalls when IAsyncInitializer.InitializeAsync()
/// throws an exception during property injection in a nested dependency chain.
/// Pattern: Test class → property (WebApplicationFactory) → nested property (AppHost) → InitializeAsync throws
/// </summary>

// Scenario 1: Nested property with IAsyncInitializer that throws during execution
public class FailingAppHost4715 : IAsyncInitializer
{
    public Task InitializeAsync()
    {
        throw new TimeoutException("Simulated: The operation has timed out (e.g., Docker container failed to start).");
    }
}

public class WebApplicationFactory4715 : IAsyncInitializer
{
    [ClassDataSource<FailingAppHost4715>(Shared = SharedType.PerTestSession)]
    public required FailingAppHost4715 AppHost { get; init; }

    public Task InitializeAsync()
    {
        // This should never be reached because AppHost.InitializeAsync() throws first
        return Task.CompletedTask;
    }
}

[EngineTest(ExpectedResult.Failure)]
public class PropertyInjectionInitFailureTests
{
    [ClassDataSource<WebApplicationFactory4715>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory4715 Factory { get; init; }

    [Test]
    public void Test_Should_Fail_Not_Stall()
    {
        // This test should never execute - it should fail during initialization
        // because the nested AppHost.InitializeAsync() throws TimeoutException
        throw new InvalidOperationException("This test should not have executed");
    }
}

// Scenario 2: Same as above but with IAsyncDiscoveryInitializer (initialized during discovery phase)
public class FailingDiscoveryAppHost4715 : IAsyncDiscoveryInitializer
{
    public Task InitializeAsync()
    {
        throw new TimeoutException("Simulated discovery init failure: The operation has timed out.");
    }
}

public class DiscoveryWebApplicationFactory4715 : IAsyncDiscoveryInitializer
{
    [ClassDataSource<FailingDiscoveryAppHost4715>(Shared = SharedType.PerTestSession)]
    public required FailingDiscoveryAppHost4715 AppHost { get; init; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}

[EngineTest(ExpectedResult.Failure)]
public class PropertyInjectionDiscoveryInitFailureTests
{
    [ClassDataSource<DiscoveryWebApplicationFactory4715>(Shared = SharedType.PerTestSession)]
    public required DiscoveryWebApplicationFactory4715 Factory { get; init; }

    [Test]
    public void Test_Should_Fail_Not_Stall_During_Discovery()
    {
        throw new InvalidOperationException("This test should not have executed");
    }
}

// Scenario 3: Multiple tests sharing the same failing factory (tests if the shared object error is handled for all)
[EngineTest(ExpectedResult.Failure)]
public class PropertyInjectionInitFailureMultiTest1
{
    [ClassDataSource<WebApplicationFactory4715>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory4715 Factory { get; init; }

    [Test]
    public void Test1_Should_Fail_Not_Stall()
    {
        throw new InvalidOperationException("This test should not have executed");
    }
}

[EngineTest(ExpectedResult.Failure)]
public class PropertyInjectionInitFailureMultiTest2
{
    [ClassDataSource<WebApplicationFactory4715>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory4715 Factory { get; init; }

    [Test]
    public void Test2_Should_Fail_Not_Stall()
    {
        throw new InvalidOperationException("This test should not have executed");
    }
}
