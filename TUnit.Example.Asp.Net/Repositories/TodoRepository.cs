using Microsoft.Extensions.Options;
using Npgsql;
using TUnit.Example.Asp.Net.Configuration;
using TUnit.Example.Asp.Net.Models;

namespace TUnit.Example.Asp.Net.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly string _connectionString;
    private readonly string _tableName;

    public TodoRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
        _tableName = options.Value.TableName;
    }

    public async Task<IEnumerable<Todo>> GetAllAsync()
    {
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

        return todos;
    }

    public async Task<Todo?> GetByIdAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT id, title, is_complete, created_at FROM \"{_tableName}\" WHERE id = @id";
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Todo
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                IsComplete = reader.GetBoolean(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }

        return null;
    }

    public async Task<Todo> CreateAsync(Todo todo)
    {
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

        return new Todo
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            IsComplete = reader.GetBoolean(2),
            CreatedAt = reader.GetDateTime(3)
        };
    }

    public async Task<Todo?> UpdateAsync(int id, Todo todo)
    {
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
            return new Todo
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                IsComplete = reader.GetBoolean(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }

        return null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM \"{_tableName}\" WHERE id = @id";
        cmd.Parameters.AddWithValue("id", id);

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
}
