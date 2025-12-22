using Microsoft.Extensions.Configuration;
using TUnit.AspNetCore;

namespace TUnit.AspNetCore.NugetTester;

/// <summary>
/// Tests verifying that service and configuration overrides work correctly when
/// TUnit.AspNetCore is consumed as a NuGet package.
/// </summary>
public class ServiceOverrideTests : TestsBase
{
    private const string TestConfigMessage = "Custom test message from configuration";

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        // Replace the greeting service with a test double
        services.ReplaceService<IGreetingService>(new TestGreetingService());

        // Replace the time service with a fixed time for deterministic testing
        services.ReplaceService<ITimeService>(new FixedTimeService(new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc)));
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        // Add test-specific configuration
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "TestMessage", TestConfigMessage }
        });
    }

    [Test]
    public async Task ServiceReplacement_UsesTestDouble()
    {
        // Verifies that the replaced IGreetingService is used
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/greet/World");
        var content = await response.Content.ReadAsStringAsync();

        // The test double should return a different format
        await Assert.That(content).IsEqualTo("Test greeting for: World");
    }

    [Test]
    public async Task ServiceReplacement_FixedTimeService_ReturnsDeterministicTime()
    {
        // Verifies that the replaced ITimeService returns the fixed time
        var timeService = Services.GetRequiredService<ITimeService>();
        var time = timeService.GetCurrentTime();

        await Assert.That(time).IsEqualTo(new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task ConfigurationOverride_AppliesTestConfiguration()
    {
        // Verifies that configuration overrides are applied
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/config/message");
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(content).IsEqualTo(TestConfigMessage);
    }

    [Test]
    public async Task ConfigurationOverride_AccessibleViaServices()
    {
        // Verifies that configuration is accessible via IConfiguration
        var config = Services.GetRequiredService<IConfiguration>();
        var message = config["TestMessage"];

        await Assert.That(message).IsEqualTo(TestConfigMessage);
    }

    /// <summary>
    /// Test double for IGreetingService that returns a different format.
    /// </summary>
    private class TestGreetingService : IGreetingService
    {
        public string GetGreeting(string name) => $"Test greeting for: {name}";
    }

    /// <summary>
    /// Test double for ITimeService that returns a fixed time.
    /// </summary>
    private class FixedTimeService : ITimeService
    {
        private readonly DateTime _fixedTime;

        public FixedTimeService(DateTime fixedTime)
        {
            _fixedTime = fixedTime;
        }

        public DateTime GetCurrentTime() => _fixedTime;
    }
}

/// <summary>
/// Tests verifying that SetupAsync can perform async operations before factory creation,
/// and those results can be used in synchronous configuration methods.
/// </summary>
public class AsyncSetupTests : TestsBase
{
    private string? _asyncResult;

    protected override async Task SetupAsync()
    {
        // Simulate async setup work (e.g., container health check, database creation)
        await Task.Delay(10);
        _asyncResult = $"async-result-{UniqueId}";
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        // Use the result from SetupAsync in the synchronous configuration
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "AsyncResult", _asyncResult }
        });
    }

    [Test]
    public async Task SetupAsync_CompletesBeforeConfigureTestConfiguration()
    {
        // Verifies that SetupAsync runs before ConfigureTestConfiguration
        await Assert.That(_asyncResult).IsNotNull();
        await Assert.That(_asyncResult).StartsWith("async-result-");
    }

    [Test]
    public async Task AsyncResultFromSetup_AvailableInConfiguration()
    {
        // Verifies that async setup results are available in configuration
        var config = Services.GetRequiredService<IConfiguration>();
        var asyncResult = config["AsyncResult"];

        await Assert.That(asyncResult).IsNotNull();
        await Assert.That(asyncResult).IsEqualTo(_asyncResult);
    }
}

/// <summary>
/// Tests verifying that test isolation works correctly - each test gets its own
/// factory and services don't leak between tests.
/// </summary>
public class IsolationTests : TestsBase
{
    private static readonly ConcurrentDictionary<string, int> SeenUniqueIds = new();

    [Test]
    [Repeat(3)]
    public async Task UniqueId_IsDifferentForEachTestInstance()
    {
        // Record the unique ID for this test instance
        var wasAdded = SeenUniqueIds.TryAdd(UniqueId.ToString(), 1);

        // Each test should have a unique ID not seen before
        await Assert.That(wasAdded).IsTrue();
    }

    [Test]
    public async Task GetIsolatedName_IncludesUniqueId()
    {
        // Verifies that isolated names include the unique ID
        var name1 = GetIsolatedName("resource");
        var name2 = GetIsolatedName("resource");

        // Same test instance should get the same isolated name for same base
        await Assert.That(name1).IsEqualTo(name2);
        await Assert.That(name1).Contains(UniqueId.ToString());
    }
}

/// <summary>
/// Tests verifying that logging integration works correctly.
/// </summary>
public class LoggingIntegrationTests : TestsBase
{
    [Test]
    public async Task TestContext_IsAccessible_DuringTest()
    {
        // Verifies that TestContext is accessible during test execution
        await Assert.That(TestContext.Current).IsNotNull();
        await Assert.That(TestContext.Current!.Metadata?.DisplayName).IsNotNull();
    }

    [Test]
    public async Task LoggerFactory_IsAvailable_InServices()
    {
        // Verifies that logging services are available
        var loggerFactory = Services.GetService<ILoggerFactory>();
        await Assert.That(loggerFactory).IsNotNull();
    }
}
