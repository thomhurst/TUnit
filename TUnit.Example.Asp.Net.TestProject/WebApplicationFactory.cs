using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject;

public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    private int _configuredWebHostCalled;

    [ClassDataSource<InMemoryRedis>(Shared = SharedType.PerTestSession)]
    public required InMemoryRedis Redis { get; init; }

    [ClassDataSource<InMemoryPostgreSqlDatabase>(Shared = SharedType.PerTestSession)]
    public required InMemoryPostgreSqlDatabase PostgreSql { get; init; }

    public int ConfiguredWebHostCalled => _configuredWebHostCalled;

    public Task InitializeAsync()
    {
        _ = Server;

        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Interlocked.Increment(ref _configuredWebHostCalled);

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Redis:ConnectionString", Redis.PostgreSqlContainer.GetConnectionString() },
                { "PostgreSql:ConnectionString", PostgreSql.PostgreSqlContainer.GetConnectionString() }
            });
        });
    }
}
