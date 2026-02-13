using Npgsql;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace CloudShop.Tests.Infrastructure;

/// <summary>
/// Provides direct database access for test verification.
/// Nested dependency: injects DistributedAppFixture to get the connection string.
/// </summary>
public class DatabaseFixture : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DistributedAppFixture>(Shared = SharedType.PerTestSession)]
    public required DistributedAppFixture App { get; init; }

    private NpgsqlDataSource? _dataSource;

    public NpgsqlDataSource DataSource => _dataSource
        ?? throw new InvalidOperationException("Database not initialized");

    public async Task InitializeAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("postgresdb");
        _dataSource = NpgsqlDataSource.Create(connectionString);

        // Verify connectivity
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1";
        await cmd.ExecuteScalarAsync();
    }

    public async Task<T?> QuerySingleAsync<T>(string sql, params (string name, object value)[] parameters)
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            cmd.Parameters.Add(param);
        }
        var result = await cmd.ExecuteScalarAsync();
        return result is DBNull or null ? default : (T)result;
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
    }
}
