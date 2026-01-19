using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Reproduction test for issue #4431 - API starting before test configuration.
///
/// User reports: "the second breakpoint (WebApi startup) gets hit first,
/// then the first one (config.AddInMemoryCollection) that basically overrides the connection string"
///
/// Expected: ConfigureTestConfiguration should run BEFORE the API starts.
/// </summary>
public class ExecutionOrderReproductionTests : WebApplicationTest<WebApplicationFactory, Program>
{
    private static readonly List<string> ExecutionLog = new();
    private static readonly object Lock = new();

    private void Log(string message)
    {
        lock (Lock)
        {
            var entry = $"{DateTime.UtcNow:HH:mm:ss.fff} - {message}";
            ExecutionLog.Add(entry);
            Console.WriteLine(entry);
        }
    }

    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        Log("1. ConfigureTestOptions called");
        base.ConfigureTestOptions(options);
    }

    protected override Task SetupAsync()
    {
        Log("2. SetupAsync called");
        return base.SetupAsync();
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        Log("5. ConfigureTestConfiguration called - THIS SHOULD BE BEFORE API STARTS");

        // This is what the user does - add configuration that should override defaults
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "TestConfigApplied", "true" },
            { "Database:ConnectionString", "test-connection-string-from-config" }
        });

        base.ConfigureTestConfiguration(config);
    }

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
        Log("6. ConfigureWebHostBuilder called");
        base.ConfigureWebHostBuilder(builder);
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        Log("7. ConfigureTestServices called");

        // Register a service that logs when the API actually starts
        services.AddHostedService<StartupLoggerService>();

        base.ConfigureTestServices(services);
    }

    [Test]
    [DisplayName("Issue #4431: Verify API starts AFTER ConfigureTestConfiguration")]
    public async Task Api_Should_Start_After_ConfigureTestConfiguration()
    {
        Log("9. Test method starting - about to call CreateClient()");

        // This should trigger the API to start
        var client = Factory.CreateClient();

        Log("10. CreateClient() returned");

        // Verify the configuration was applied
        var config = Factory.Services.GetRequiredService<IConfiguration>();
        var testConfigApplied = config["TestConfigApplied"];

        await Assert.That(testConfigApplied).IsEqualTo("true")
            .Because("Test configuration should have been applied before API started");

        // Print the execution log
        Console.WriteLine("\n=== FULL EXECUTION LOG ===");
        foreach (var entry in ExecutionLog)
        {
            Console.WriteLine(entry);
        }
        Console.WriteLine("===========================\n");
    }

    [Test]
    [DisplayName("Issue #4431: Verify connection string override works")]
    public async Task Connection_String_Should_Be_Overridden()
    {
        Log("Test: Connection string override test starting");

        var client = Factory.CreateClient();

        var config = Factory.Services.GetRequiredService<IConfiguration>();
        var connectionString = config["Database:ConnectionString"];

        // The test's ConfigureTestConfiguration should have overridden any default
        await Assert.That(connectionString).IsEqualTo("test-connection-string-from-config")
            .Because("Test configuration should override factory defaults");
    }
}

/// <summary>
/// A hosted service that logs when the application actually starts.
/// This helps us verify the startup timing.
/// </summary>
public class StartupLoggerService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} - 8. API STARTUP - StartupLoggerService.StartAsync called");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} - API SHUTDOWN - StartupLoggerService.StopAsync called");
        return Task.CompletedTask;
    }
}
