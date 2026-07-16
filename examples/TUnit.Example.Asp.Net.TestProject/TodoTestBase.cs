using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Base class for Todo API tests with per-test table isolation.
/// Extends TestsBase (which provides container injection) and adds:
/// - Unique table name per test (using GetIsolatedName helper)
/// - Table creation/cleanup
/// - Seeding helpers
/// </summary>
/// <remarks>
/// This class demonstrates the async setup pattern:
/// - <see cref="SetupAsync"/> runs before factory creation for async operations
/// - <see cref="ConfigureTestConfiguration"/> uses results from SetupAsync
/// - <see cref="GetIsolatedName"/> provides consistent isolation naming
/// </remarks>
[SuppressMessage("Usage", "TUnit0043:Property must use `required` keyword")]
public abstract class TodoTestBase : TestsBase
{
    [ClassDataSource<InMemoryPostgreSqlDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgreSqlDatabase PostgreSql { get; init; } = null!;

    /// <summary>
    /// The unique table name for this test.
    /// </summary>
    protected string TableName { get; private set; } = null!;

    /// <summary>
    /// Performs async setup: generates unique table name and creates the table.
    /// Runs BEFORE ConfigureTestConfiguration, so TableName is ready for use.
    /// </summary>
    protected override async Task SetupAsync()
    {
        // Generate unique table name using the built-in helper
        TableName = GetIsolatedName("todos");

        // Create the table - this is async!
        await CreateTableAsync(TableName);
    }

    /// <summary>
    /// Configures the application with the unique table name.
    /// TableName is already set by SetupAsync.
    /// </summary>
    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Database:TableName", TableName }
        });
    }

    [After(HookType.Test)]
    public async Task CleanupTable()
    {
        if (!string.IsNullOrEmpty(TableName))
        {
            await DropTableAsync(TableName);
        }
    }

    /// <summary>
    /// Creates the table for this test.
    /// </summary>
    protected async Task CreateTableAsync(string tableName)
    {
        await using var connection = new NpgsqlConnection(PostgreSql.Container.GetConnectionString());
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            CREATE TABLE IF NOT EXISTS "{tableName}" (
                id SERIAL PRIMARY KEY,
                title TEXT NOT NULL,
                is_complete BOOLEAN DEFAULT FALSE,
                created_at TIMESTAMP DEFAULT NOW()
            )
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops the table after the test.
    /// </summary>
    protected async Task DropTableAsync(string tableName)
    {
        await using var connection = new NpgsqlConnection(PostgreSql.Container.GetConnectionString());
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS \"{tableName}\"";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Seeds the database with todos directly (bypassing the API).
    /// </summary>
    protected async Task SeedTodosAsync(params string[] titles)
    {
        await using var connection = new NpgsqlConnection(PostgreSql.Container.GetConnectionString());
        await connection.OpenAsync();

        foreach (var title in titles)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"INSERT INTO \"{TableName}\" (title) VALUES (@title)";
            cmd.Parameters.AddWithValue("title", title);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Seeds a completed todo directly to the database.
    /// </summary>
    protected async Task SeedCompletedTodoAsync(string title)
    {
        await using var connection = new NpgsqlConnection(PostgreSql.Container.GetConnectionString());
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"INSERT INTO \"{TableName}\" (title, is_complete) VALUES (@title, TRUE)";
        cmd.Parameters.AddWithValue("title", title);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets the count of todos directly from the database.
    /// </summary>
    protected async Task<int> GetTodoCountAsync()
    {
        await using var connection = new NpgsqlConnection(PostgreSql.Container.GetConnectionString());
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM \"{TableName}\"";
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}
