namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Tests verifying that server startup is tracked independently for each test.
/// Each test gets its own IStartupFilter instance that records when that test's
/// server actually starts processing the pipeline.
/// </summary>
public class PerTestServerStartupTests : TestsBase
{
    [Test]
    [DisplayName("Server startup is tracked per-test (StartupCalledOrder is non-zero)")]
    public async Task Server_Startup_Is_Tracked_Per_Test()
    {
        // Trigger server startup by creating a client
        _ = Factory.CreateClient();

        // Verify that startup was tracked for this specific test instance
        await Assert.That(StartupCalledOrder).IsGreaterThan(0)
            .Because("Each test should track its own server startup order");

        await Assert.That(StartupCalledAt).IsNotNull()
            .Because("StartupCalledAt timestamp should be recorded");
    }

    [Test]
    [DisplayName("Server startup time is recorded accurately")]
    public async Task Server_Startup_Time_Is_Recorded()
    {
        // Add small buffer to account for DateTime precision differences
        var beforeCall = DateTime.UtcNow.AddMilliseconds(-100);

        _ = Factory.CreateClient();

        var afterCall = DateTime.UtcNow.AddMilliseconds(100);

        await Assert.That(StartupCalledAt).IsNotNull();
        await Assert.That(StartupCalledAt!.Value).IsGreaterThanOrEqualTo(beforeCall)
            .Because("Server startup should occur around when we call CreateClient");
        await Assert.That(StartupCalledAt!.Value).IsLessThanOrEqualTo(afterCall)
            .Because("Server startup should complete around when CreateClient returns");
    }

    [Test]
    [DisplayName("Server startup runs after all configuration hooks")]
    public async Task Server_Startup_Runs_After_Configuration()
    {
        _ = Factory.CreateClient();

        // Server startup should be the last step in the lifecycle
        await Assert.That(StartupCalledOrder)
            .IsGreaterThan(ConfigureTestOptionsCalledOrder)
            .Because("Server starts after ConfigureTestOptions");

        await Assert.That(StartupCalledOrder)
            .IsGreaterThan(SetupCalledOrder)
            .Because("Server starts after SetupAsync");

        await Assert.That(StartupCalledOrder)
            .IsGreaterThan(ConfigureTestConfigurationCalledOrder)
            .Because("Server starts after ConfigureTestConfiguration");

        await Assert.That(StartupCalledOrder)
            .IsGreaterThan(ConfigureWebHostBuilderCalledOrder)
            .Because("Server starts after ConfigureWebHostBuilder");

        await Assert.That(StartupCalledOrder)
            .IsGreaterThan(ConfigureTestServicesCalledOrder)
            .Because("Server starts after ConfigureTestServices");
    }

    [Test]
    [DisplayName("Multiple CreateClient calls don't re-trigger startup")]
    public async Task Multiple_CreateClient_Calls_Use_Same_Startup()
    {
        // First call triggers startup
        _ = Factory.CreateClient();
        var firstStartupOrder = StartupCalledOrder;
        var firstStartupAt = StartupCalledAt;

        // Second call should use existing server
        _ = Factory.CreateClient();

        // Startup order and time should be unchanged
        await Assert.That(StartupCalledOrder).IsEqualTo(firstStartupOrder)
            .Because("Startup should only be tracked once per test");

        await Assert.That(StartupCalledAt).IsEqualTo(firstStartupAt)
            .Because("Startup timestamp should not change on subsequent CreateClient calls");
    }

    [Test]
    [DisplayName("Server is accessible after startup")]
    public async Task Server_Is_Accessible_After_Startup()
    {
        var client = Factory.CreateClient();

        // Verify server started
        await Assert.That(StartupCalledOrder).IsGreaterThan(0);

        // Verify server is functional
        var response = await client.GetAsync("/ping");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).IsEqualTo("Hello, World!");
    }
}
