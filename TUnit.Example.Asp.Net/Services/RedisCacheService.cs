using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TUnit.Example.Asp.Net.Configuration;

namespace TUnit.Example.Asp.Net.Services;

public class RedisCacheService : ICacheService, IAsyncDisposable
{
    private readonly RedisOptions _options;
    private readonly ILogger<RedisCacheService> _logger;
    private ConnectionMultiplexer? _connection;
    private IDatabase? _database;

    public RedisCacheService(IOptions<RedisOptions> options, ILogger<RedisCacheService> logger)
    {
        _options = options.Value;
        _logger = logger;
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
        var prefixedKey = GetPrefixedKey(key);
        _logger.LogDebug("Getting cache key {Key}", prefixedKey);

        var db = await GetDatabaseAsync();
        var value = await db.StringGetAsync(prefixedKey);

        if (value.HasValue)
        {
            _logger.LogInformation("Cache hit for key {Key}", prefixedKey);
            return value.ToString();
        }

        _logger.LogDebug("Cache miss for key {Key}", prefixedKey);
        return null;
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        var prefixedKey = GetPrefixedKey(key);
        _logger.LogDebug("Setting cache key {Key}", prefixedKey);

        var db = await GetDatabaseAsync();
        await db.StringSetAsync(prefixedKey, value, expiry);

        _logger.LogInformation("Cached value for key {Key}, expiry={Expiry}", prefixedKey, expiry?.ToString() ?? "none");
    }

    public async Task<bool> DeleteAsync(string key)
    {
        var prefixedKey = GetPrefixedKey(key);
        _logger.LogDebug("Deleting cache key {Key}", prefixedKey);

        var db = await GetDatabaseAsync();
        var deleted = await db.KeyDeleteAsync(prefixedKey);

        if (deleted)
        {
            _logger.LogInformation("Deleted cache key {Key}", prefixedKey);
        }
        else
        {
            _logger.LogDebug("Cache key {Key} not found for deletion", prefixedKey);
        }

        return deleted;
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var prefixedKey = GetPrefixedKey(key);
        var db = await GetDatabaseAsync();
        var exists = await db.KeyExistsAsync(prefixedKey);
        _logger.LogDebug("Cache key {Key} exists: {Exists}", prefixedKey, exists);
        return exists;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
