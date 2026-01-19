using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.AspNetCore;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Base class for ASP.NET Core integration tests using the WebApplicationTest pattern.
/// Provides shared container injection and configuration for all test classes.
/// </summary>
public abstract class TestsBase : WebApplicationTest<WebApplicationFactory, Program>
{
    // Instance-based order counter to avoid interference from parallel tests
    private int _orderCounter;

    /// <summary>
    /// Gets the next order value for this test instance. Thread-safe for async operations.
    /// </summary>
    internal int GetNextOrder() => Interlocked.Increment(ref _orderCounter);

    // Order tracking for test hooks (more precise than DateTime for fast operations)
    public int SetupCalledOrder { get; private set; }
    public int ConfigureTestOptionsCalledOrder { get; private set; }
    public int ConfigureTestServicesCalledOrder { get; private set; }
    public int ConfigureTestConfigurationCalledOrder { get; private set; }
    public int ConfigureWebHostBuilderCalledOrder { get; private set; }
    public int StartupCalledOrder { get; set; }

    // Order tracking for factory methods (exposed from the global factory)
    public int FactoryConfigureWebHostCalledOrder => GlobalFactory.ConfigureWebHostCalledOrder;
    public int FactoryConfigureStartupConfigurationCalledOrder => GlobalFactory.ConfigureStartupConfigurationCalledOrder;

    // DateTime tracking (kept for reference, but order is more reliable)
    public DateTime? SetupCalledAt { get; private set; }
    public DateTime? ConfigureTestOptionsCalledAt { get; private set; }
    public DateTime? ConfigureTestServicesCalledAt { get; private set; }
    public DateTime? ConfigureTestConfigurationCalledAt { get; private set; }
    public DateTime? ConfigureWebHostBuilderCalledAt { get; private set; }
    public DateTime? StartupCalledAt { get; set; }

    protected override Task SetupAsync()
    {
        // Counter starts at 0 for each new test instance
        SetupCalledOrder = GetNextOrder();
        SetupCalledAt = DateTime.UtcNow;
        return base.SetupAsync();
    }

    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        ConfigureTestOptionsCalledOrder = GetNextOrder();
        ConfigureTestOptionsCalledAt = DateTime.UtcNow;

        // Wire up the factory's order tracking callback to use this test's counter
        GlobalFactory.GetNextOrderCallback = GetNextOrder;

        base.ConfigureTestOptions(options);
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        ConfigureTestServicesCalledOrder = GetNextOrder();
        ConfigureTestServicesCalledAt = DateTime.UtcNow;
        base.ConfigureTestServices(services);
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        // Track first call only (this method is called twice - once for startup config, once for app config)
        if (ConfigureTestConfigurationCalledOrder == 0)
        {
            ConfigureTestConfigurationCalledOrder = GetNextOrder();
            ConfigureTestConfigurationCalledAt = DateTime.UtcNow;
        }

        base.ConfigureTestConfiguration(config);
    }

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
        ConfigureWebHostBuilderCalledOrder = GetNextOrder();
        ConfigureWebHostBuilderCalledAt = DateTime.UtcNow;
        base.ConfigureWebHostBuilder(builder);

        // Register the startup filter as a service to hook into the startup pipeline
        // This wraps the configure pipeline without replacing it
        builder.ConfigureServices(services =>
            services.AddSingleton<IStartupFilter>(new StartupFilter(this)));
    }
}
