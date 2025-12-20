using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TUnit.Example.Asp.Net.Configuration;

namespace TUnit.Example.Asp.Net.Services;

public class RedisCacheService : ICacheService, IAsyncDisposable
{
    private readonly RedisOptions _options;
    private ConnectionMultiplexer? _connection;
    private IDatabase? _database;

    public RedisCacheService(IOptions<RedisOptions> options)
    {
        _options = options.Value;
    }

    private async Task<IDatabase> GetDatabaseAsync()
    {
        if (_database is not null)
        {
            return _database;
        }

        _connection = await ConnectionMultiplexer.ConnectAsync(_options.ConnectionString);
        _database = _connection.GetDatabase();
        return _database;
    }

    private string GetPrefixedKey(string key) =>
        string.IsNullOrEmpty(_options.KeyPrefix) ? key : $"{_options.KeyPrefix}{key}";

    public async Task<string?> GetAsync(string key)
    {
        var db = await GetDatabaseAsync();
        var value = await db.StringGetAsync(GetPrefixedKey(key));
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        var db = await GetDatabaseAsync();
        await db.StringSetAsync(GetPrefixedKey(key), value, expiry);
    }

    public async Task<bool> DeleteAsync(string key)
    {
        var db = await GetDatabaseAsync();
        return await db.KeyDeleteAsync(GetPrefixedKey(key));
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var db = await GetDatabaseAsync();
        return await db.KeyExistsAsync(GetPrefixedKey(key));
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
