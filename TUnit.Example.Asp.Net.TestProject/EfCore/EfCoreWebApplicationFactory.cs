using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.AspNetCore;
using TUnit.Example.Asp.Net.EfCore;

namespace TUnit.Example.Asp.Net.TestProject.EfCore;

/// <summary>
/// WebApplicationFactory configured with EF Core services.
/// Reuses the shared PostgreSQL container and adds DbContext registration.
/// </summary>
[SuppressMessage("Usage", "TUnit0043:Property must use `required` keyword")]
public class EfCoreWebApplicationFactory : TestWebApplicationFactory<Program>
{
    [ClassDataSource<InMemoryPostgreSqlDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgreSqlDatabase PostgreSql { get; init; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove any existing DbContext registration from the app
            services.RemoveAll<DbContextOptions<TodoDbContext>>();

            // Register with the container's connection string and schema-aware caching
            services.AddDbContext<TodoDbContext>(options =>
                options.UseNpgsql(PostgreSql.Container.GetConnectionString())
                    .ReplaceService<IModelCacheKeyFactory, SchemaModelCacheKeyFactory>());
        });
    }

    protected override void ConfigureStartupConfiguration(
        Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "SomeKey", "SomeValue" },
            { "Database:ConnectionString", PostgreSql.Container.GetConnectionString() }
        });
    }
}
