using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Factory for integration tests. Extends TestWebApplicationFactory to support
/// per-test isolation via the WebApplicationTest pattern.
///
/// Note: This factory intentionally does NOT have required container properties.
/// Instead, test classes inject containers and provide configuration overrides
/// via OverrideConfigurationAsync. This allows the factory to be created with new().
/// </summary>
public class WebApplicationFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    private int _configuredWebHostCalled;

    public int ConfiguredWebHostCalled => _configuredWebHostCalled;

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


    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Interlocked.Increment(ref _configuredWebHostCalled);

        // Base configuration - tests will override with actual container connection strings
        // via OverrideConfigurationAsync
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            // Defaults that will be overridden by tests
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", PostgreSql.Container.GetConnectionString() },
                { "Redis:ConnectionString", Redis.Container.GetConnectionString() },
                { "Kafka:ConnectionString", Kafka.Container.GetBootstrapAddress() }
            });
        });
    }
}
