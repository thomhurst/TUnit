using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TUnit.AspNetCore;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Reproduction and explanation of issue #4431.
///
/// User reports: breakpoint in WebApi.Program.cs gets hit BEFORE
/// breakpoint in ConfigureWebHost's ConfigureAppConfiguration.
///
/// ROOT CAUSE: This is EXPECTED behavior of ASP.NET Core's WebApplicationFactory.
/// The ConfigureAppConfiguration callback registered in ConfigureWebHost is DEFERRED
/// and runs AFTER the app's Program.cs code, not before.
///
/// SOLUTION: Use ConfigureStartupConfiguration instead, which uses UseSetting()
/// to apply configuration BEFORE the app's Program.cs code runs.
/// </summary>

// ============================================================================
// BROKEN APPROACH - What the user was doing
// ============================================================================

/// <summary>
/// This factory demonstrates the BROKEN approach.
/// ConfigureWebHost + ConfigureAppConfiguration callbacks are DEFERRED
/// and run AFTER the app's Program.cs, causing the "SomeKey" check to fail.
/// </summary>
public class Issue4431BrokenFactory : TestWebApplicationFactory<Program>
{
    [ClassDataSource<InMemoryPostgreSqlDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgreSqlDatabase Postgres { get; init; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // This log shows ConfigureWebHost METHOD is called...
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} - BROKEN FACTORY: ConfigureWebHost called");

        // ...but this callback is DEFERRED and runs AFTER Program.cs!
        builder.ConfigureAppConfiguration((_, config) =>
        {
            Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} - BROKEN FACTORY: ConfigureAppConfiguration callback executing");
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", Postgres.Container.GetConnectionString() },
                { "Issue4431:FactoryConfigured", "true" }
            });
        });
    }

    // NOTE: No ConfigureStartupConfiguration override - that's the problem!
}

// ============================================================================
// WORKING APPROACH - The correct solution
// ============================================================================

/// <summary>
/// This factory demonstrates the CORRECT approach.
/// ConfigureStartupConfiguration uses UseSetting() which applies configuration
/// BEFORE the app's Program.cs runs.
/// </summary>
public class Issue4431WorkingFactory : TestWebApplicationFactory<Program>
{
    [ClassDataSource<InMemoryPostgreSqlDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgreSqlDatabase Postgres { get; init; } = null!;

    /// <summary>
    /// ConfigureStartupConfiguration runs BEFORE Program.cs.
    /// Use this for any configuration that Program.cs needs during startup.
    /// </summary>
    protected override void ConfigureStartupConfiguration(IConfigurationBuilder configurationBuilder)
    {
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} - WORKING FACTORY: ConfigureStartupConfiguration called");

        // This configuration is available when Program.cs runs
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "SomeKey", "SomeValue" },  // Required by Program.cs check
            { "Database:ConnectionString", Postgres.Container.GetConnectionString() },
            { "Issue4431:FactoryConfigured", "true" }
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} - WORKING FACTORY: ConfigureWebHost called");
        // ConfigureWebHost can still be used for other customizations
        // Just don't use ConfigureAppConfiguration for configuration needed during startup
    }
}

// ============================================================================
// TESTS
// ============================================================================

/// <summary>
/// Tests demonstrating the BROKEN approach.
///
/// NOTE: The test below is SKIPPED because the exception occurs in a BeforeTest hook
/// (when initializing the factory), not in the test body. This is exactly what
/// demonstrates the problem - the app CANNOT start because the configuration
/// isn't available when Program.cs runs.
///
/// To see the failure, remove the [Skip] attribute and run the test.
/// </summary>
public class Issue4431BrokenTests : WebApplicationTest<Issue4431BrokenFactory, Program>
{
    [Test]
    [Skip("This test demonstrates the broken approach - it fails in BeforeTest hook because ConfigureAppConfiguration runs AFTER Program.cs")]
    [DisplayName("Issue #4431 BROKEN: ConfigureAppConfiguration runs AFTER Program.cs (EXPECTED TO FAIL)")]
    public async Task BrokenApproach_Fails_Because_ConfigurationRunsTooLate()
    {
        // This would throw InvalidOperationException("SomeKey is not SomeValue")
        // because Program.cs checks SomeKey before ConfigureAppConfiguration runs.
        //
        // However, the exception happens in the BeforeTest hook (InitializeFactoryAsync),
        // not in the test body, so we can't catch it here.
        //
        // This demonstrates the problem: you CANNOT use ConfigureWebHost + ConfigureAppConfiguration
        // for configuration that your app's Program.cs needs during startup.
        _ = Factory.CreateClient();
        await Task.CompletedTask;
    }
}

/// <summary>
/// Tests demonstrating the WORKING approach - these tests PASS because
/// ConfigureStartupConfiguration runs before Program.cs.
/// </summary>
public class Issue4431WorkingTests : WebApplicationTest<Issue4431WorkingFactory, Program>
{
    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} - TEST: ConfigureTestConfiguration called");
        base.ConfigureTestConfiguration(config);
    }

    [Test]
    [DisplayName("Issue #4431 WORKING: ConfigureStartupConfiguration runs BEFORE Program.cs")]
    public async Task WorkingApproach_Succeeds_Because_ConfigurationRunsBeforeStartup()
    {
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} - TEST: About to call Factory.CreateClient()");

        // This succeeds because ConfigureStartupConfiguration set SomeKey before Program.cs ran
        var client = Factory.CreateClient();

        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} - TEST: CreateClient() returned");

        // Verify that factory configuration was applied
        var config = Factory.Services.GetRequiredService<IConfiguration>();
        var factoryConfigured = config["Issue4431:FactoryConfigured"];

        await Assert.That(factoryConfigured).IsEqualTo("true")
            .Because("ConfigureStartupConfiguration runs before Program.cs and sets this value");

        var response = await client.GetAsync("/ping");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    [DisplayName("Issue #4431 WORKING: Database connection string is set correctly")]
    public async Task DatabaseConnectionString_IsSetCorrectly()
    {
        var client = Factory.CreateClient();

        var config = Factory.Services.GetRequiredService<IConfiguration>();
        var connectionString = config["Database:ConnectionString"];

        await Assert.That(connectionString).IsNotNull()
            .Because("ConfigureStartupConfiguration sets the connection string from the container");

        await Assert.That(connectionString!).Contains("Host=")
            .Because("PostgreSQL connection strings contain 'Host='");
    }
}
