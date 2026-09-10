using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using TUnit.Example.Asp.Net.Configuration;
using TUnit.Example.Asp.Net.Models;

namespace TUnit.Example.Asp.Net.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly ILogger<TodoRepository> _logger;

    public TodoRepository(IOptions<DatabaseOptions> options, ILogger<TodoRepository> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _tableName = options.Value.TableName;
        _logger = logger;
    }

    public async Task<IEnumerable<Todo>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all todos from table {TableName}", _tableName);

        var todos = new List<Todo>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT id, title, is_complete, created_at FROM \"{_tableName}\" ORDER BY created_at DESC";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            todos.Add(new Todo
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                IsComplete = reader.GetBoolean(2),
                CreatedAt = reader.GetDateTime(3)
            });
        }

        _logger.LogInformation("Retrieved {Count} todos from table {TableName}", todos.Count, _tableName);
        return todos;
    }

    public async Task<Todo?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Fetching todo {TodoId} from table {TableName}", id, _tableName);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT id, title, is_complete, created_at FROM \"{_tableName}\" WHERE id = @id";
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var todo = new Todo
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                IsComplete = reader.GetBoolean(2),
                CreatedAt = reader.GetDateTime(3)
            };
            _logger.LogInformation("Found todo {TodoId}: {Title}", todo.Id, todo.Title);
            return todo;
        }

        _logger.LogWarning("Todo {TodoId} not found in table {TableName}", id, _tableName);
        return null;
    }

    public async Task<Todo> CreateAsync(Todo todo)
    {
        _logger.LogDebug("Creating todo with title {Title} in table {TableName}", todo.Title, _tableName);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            INSERT INTO "{_tableName}" (title, is_complete, created_at)
            VALUES (@title, @is_complete, @created_at)
            RETURNING id, title, is_complete, created_at
            """;
        cmd.Parameters.AddWithValue("title", todo.Title);
        cmd.Parameters.AddWithValue("is_complete", todo.IsComplete);
        cmd.Parameters.AddWithValue("created_at", todo.CreatedAt);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        var created = new Todo
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            IsComplete = reader.GetBoolean(2),
            CreatedAt = reader.GetDateTime(3)
        };

        _logger.LogInformation("Created todo {TodoId}: {Title}", created.Id, created.Title);
        return created;
    }

    public async Task<Todo?> UpdateAsync(int id, Todo todo)
    {
        _logger.LogDebug("Updating todo {TodoId} in table {TableName}", id, _tableName);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            UPDATE "{_tableName}"
            SET title = @title, is_complete = @is_complete
            WHERE id = @id
            RETURNING id, title, is_complete, created_at
            """;
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("title", todo.Title);
        cmd.Parameters.AddWithValue("is_complete", todo.IsComplete);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var updated = new Todo
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                IsComplete = reader.GetBoolean(2),
                CreatedAt = reader.GetDateTime(3)
            };
            _logger.LogInformation("Updated todo {TodoId}: {Title}, IsComplete={IsComplete}", updated.Id, updated.Title, updated.IsComplete);
            return updated;
        }

        _logger.LogWarning("Failed to update todo {TodoId} - not found in table {TableName}", id, _tableName);
        return null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogDebug("Deleting todo {TodoId} from table {TableName}", id, _tableName);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM \"{_tableName}\" WHERE id = @id";
        cmd.Parameters.AddWithValue("id", id);

        var rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            _logger.LogInformation("Deleted todo {TodoId} from table {TableName}", id, _tableName);
            return true;
        }

        _logger.LogWarning("Failed to delete todo {TodoId} - not found in table {TableName}", id, _tableName);
        return false;
    }
}
