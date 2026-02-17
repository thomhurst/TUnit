using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TUnit.AspNetCore;
using TUnit.Example.Asp.Net.EfCore;

namespace TUnit.Example.Asp.Net.TestProject.EfCore;

/// <summary>
/// Base class for EF Core integration tests with per-test schema isolation.
/// Each test gets a unique PostgreSQL schema, with tables created via EF Core's
/// <see cref="RelationalDatabaseCreator.EnsureCreatedAsync"/>.
/// </summary>
/// <remarks>
/// This demonstrates the recommended pattern for EF Core Code First testing:
/// <list type="number">
///   <item><see cref="SetupAsync"/> creates a unique schema and runs EnsureCreated</item>
///   <item><see cref="ConfigureTestConfiguration"/> passes the schema to the app</item>
///   <item>Each test runs against its own isolated schema</item>
///   <item><see cref="CleanupSchema"/> drops the schema after the test</item>
/// </list>
/// </remarks>
[SuppressMessage("Usage", "TUnit0043:Property must use `required` keyword")]
public abstract class EfCoreTodoTestBase : WebApplicationTest<EfCoreWebApplicationFactory, Program>
{
    [ClassDataSource<InMemoryPostgreSqlDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgreSqlDatabase PostgreSql { get; init; } = null!;

    /// <summary>
    /// The unique schema name for this test.
    /// </summary>
    protected string SchemaName { get; private set; } = null!;

    /// <summary>
    /// Creates a unique schema and initializes EF Core tables within it.
    /// Runs BEFORE ConfigureTestConfiguration, so SchemaName is ready for use.
    /// </summary>
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

    /// <summary>
    /// Passes the test-specific schema name to the application.
    /// </summary>
    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Database:Schema", SchemaName }
        });
    }

    /// <summary>
    /// Drops the test-specific schema and all its tables after the test.
    /// </summary>
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
    /// Creates a TodoDbContext scoped to this test's schema.
    /// The caller is responsible for disposing the returned context.
    /// </summary>
    protected TodoDbContext CreateDbContext()
    {
        var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        dbContext.SchemaName = SchemaName;
        return dbContext;
    }

    /// <summary>
    /// Seeds the database with todos directly via EF Core (bypassing the API).
    /// </summary>
    protected async Task SeedTodosAsync(params string[] titles)
    {
        await using var dbContext = CreateDbContext();
        foreach (var title in titles)
        {
            dbContext.Todos.Add(new Example.Asp.Net.Models.Todo { Title = title });
        }
        await dbContext.SaveChangesAsync();
    }
}
