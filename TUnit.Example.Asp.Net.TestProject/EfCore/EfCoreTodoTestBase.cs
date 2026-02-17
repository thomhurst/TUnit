using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TUnit.AspNetCore;
using TUnit.Example.Asp.Net.EfCore;
using TUnit.Example.Asp.Net.Models;

namespace TUnit.Example.Asp.Net.TestProject.EfCore;

/// <summary>
/// Base class for EF Core integration tests with per-test schema isolation.
/// Each test gets a unique PostgreSQL schema, with tables created via
/// EF Core's EnsureCreatedAsync.
/// </summary>
[SuppressMessage("Usage", "TUnit0043:Property must use `required` keyword")]
public abstract class EfCoreTodoTestBase : WebApplicationTest<EfCoreWebApplicationFactory, Program>
{
    [ClassDataSource<InMemoryPostgreSqlDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgreSqlDatabase PostgreSql { get; init; } = null!;

    protected string SchemaName { get; private set; } = null!;

    protected override async Task SetupAsync()
    {
        SchemaName = GetIsolatedName("schema");

        // Create the schema via raw SQL
        await using var connection = new NpgsqlConnection(PostgreSql.Container.GetConnectionString());
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{SchemaName}\"";
        await cmd.ExecuteNonQueryAsync();

        // Use EF Core to create tables in the new schema
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseNpgsql(PostgreSql.Container.GetConnectionString())
            .ReplaceService<IModelCacheKeyFactory, SchemaModelCacheKeyFactory>()
            .Options;

        await using var dbContext = new TodoDbContext(options) { SchemaName = SchemaName };
        await dbContext.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Database:Schema", SchemaName }
        });
    }

    [After(HookType.Test)]
    public async Task CleanupSchema()
    {
        if (string.IsNullOrEmpty(SchemaName))
        {
            return;
        }

        await using var connection = new NpgsqlConnection(PostgreSql.Container.GetConnectionString());
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DROP SCHEMA IF EXISTS \"{SchemaName}\" CASCADE";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates a scoped TodoDbContext for direct database access in tests.
    /// The context reads Database:Schema from the per-test configuration automatically.
    /// </summary>
    protected AsyncServiceScope CreateDbScope(out TodoDbContext dbContext)
    {
        var scope = Factory.Services.CreateAsyncScope();
        dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        return scope;
    }

    /// <summary>
    /// Seeds the database with todos directly via EF Core (bypassing the API).
    /// </summary>
    protected async Task SeedTodosAsync(params string[] titles)
    {
        await using var scope = CreateDbScope(out var dbContext);
        foreach (var title in titles)
        {
            dbContext.Todos.Add(new Todo { Title = title });
        }
        await dbContext.SaveChangesAsync();
    }
}
