using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TUnit.AspNetCore;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Factory for integration tests. Extends TestWebApplicationFactory to support
/// per-test isolation via the WebApplicationTest pattern.
///
/// Note: This factory intentionally does NOT have required container properties.
/// Instead, test classes inject containers and provide configuration overrides
/// via OverrideConfigurationAsync. This allows the factory to be created with new().
/// </summary>
public class WebApplicationFactory : TestWebApplicationFactory<Program>
{
    private int _configureWebHostCallCount;
    private int _configureStartupConfigurationCallCount;

    /// <summary>
    /// Number of times ConfigureWebHost has been called.
    /// </summary>
    public int ConfigureWebHostCallCount => _configureWebHostCallCount;

    /// <summary>
    /// Number of times ConfigureStartupConfiguration has been called.
    /// </summary>
    public int ConfigureStartupConfigurationCallCount => _configureStartupConfigurationCallCount;

    /// <summary>
    /// Order tracking callback - set by test to track when factory methods are called.
    /// </summary>
    public Func<int>? GetNextOrderCallback { get; set; }

    /// <summary>
    /// Order when ConfigureWebHost was called.
    /// </summary>
    public int ConfigureWebHostCalledOrder { get; private set; }

    /// <summary>
    /// Order when ConfigureStartupConfiguration was called.
    /// </summary>
    public int ConfigureStartupConfigurationCalledOrder { get; private set; }

    /// <summary>
    /// PostgreSQL container - shared across all tests in the session.
    /// </summary>
    [ClassDataSource<InMemoryPostgreSqlDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgreSqlDatabase PostgreSql { get; init; } = null!;

    /// <summary>
    /// Redis container - shared across all tests in the session.
    /// </summary>
    [ClassDataSource<InMemoryRedis>(Shared = SharedType.PerTestSession)]
    public InMemoryRedis Redis { get; init; } = null!;

    /// <summary>
    /// Kafka container - shared across all tests in the session.
    /// </summary>
    [ClassDataSource<InMemoryKafka>(Shared = SharedType.PerTestSession)]
    public InMemoryKafka Kafka { get; init; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Interlocked.Increment(ref _configureWebHostCallCount);
        if (GetNextOrderCallback != null && ConfigureWebHostCalledOrder == 0)
        {
            ConfigureWebHostCalledOrder = GetNextOrderCallback();
        }

        // Base configuration - tests will override with actual container connection strings
        // via OverrideConfigurationAsync
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            // Defaults that will be overridden by tests
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", PostgreSql.Container.GetConnectionString() },
                { "Redis:ConnectionString", Redis.Container.GetConnectionString() },
                { "Kafka:ConnectionString", Kafka.Container.GetBootstrapAddress() },
            });
        });
    }

    protected override void ConfigureStartupConfiguration(IConfigurationBuilder configurationBuilder)
    {
        Interlocked.Increment(ref _configureStartupConfigurationCallCount);
        if (GetNextOrderCallback != null && ConfigureStartupConfigurationCalledOrder == 0)
        {
            ConfigureStartupConfigurationCalledOrder = GetNextOrderCallback();
        }

        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "SomeKey", "SomeValue" }
        });
    }
}
