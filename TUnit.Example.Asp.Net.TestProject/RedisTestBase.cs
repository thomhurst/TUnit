using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Base class for Redis tests with per-test key prefix isolation.
/// Extends TestsBase (which provides container injection) and adds:
/// - Unique key prefix per test (using GetIsolatedPrefix helper)
/// - Key cleanup after tests
/// </summary>
/// <remarks>
/// This class demonstrates the new simpler configuration API:
/// - <see cref="ConfigureTestConfiguration"/> is auto-additive (no need to call base)
/// - <see cref="GetIsolatedPrefix"/> provides consistent isolation naming
/// </remarks>
[SuppressMessage("Usage", "TUnit0043:Property must use `required` keyword")]
public abstract class RedisTestBase : TestsBase
{
    [ClassDataSource<InMemoryRedis>(Shared = SharedType.PerTestSession)]
    public InMemoryRedis Redis { get; init; } = null!;

    /// <summary>
    /// The unique key prefix for this test.
    /// All cache operations will use keys prefixed with this value.
    /// </summary>
    protected string KeyPrefix { get; private set; } = null!;

    /// <summary>
    /// Configures the application with a unique key prefix for this test.
    /// Uses the new simpler API - no need to call base or chain delegates.
    /// </summary>
    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        // Generate unique key prefix using the built-in helper
        KeyPrefix = GetIsolatedPrefix();

        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Redis:KeyPrefix", KeyPrefix }
        });
    }

    [After(HookType.Test)]
    public async Task CleanupRedisKeys()
    {
        if (string.IsNullOrEmpty(KeyPrefix))
        {
            return;
        }

        try
        {
            await using var connection = await ConnectionMultiplexer.ConnectAsync(
                Redis.Container.GetConnectionString());
            var server = connection.GetServer(connection.GetEndPoints()[0]);
            var db = connection.GetDatabase();

            // Find and delete all keys with our prefix
            await foreach (var key in server.KeysAsync(pattern: $"{KeyPrefix}*"))
            {
                await db.KeyDeleteAsync(key);
            }
        }
        catch
        {
            // Cleanup failures shouldn't fail the test
        }
    }

    /// <summary>
    /// Gets a value directly from Redis (bypassing the API).
    /// </summary>
    protected async Task<string?> GetRawValueAsync(string key)
    {
        await using var connection = await ConnectionMultiplexer.ConnectAsync(
            Redis.Container.GetConnectionString());
        var db = connection.GetDatabase();
        var value = await db.StringGetAsync($"{KeyPrefix}{key}");
        return value.HasValue ? value.ToString() : null;
    }

    /// <summary>
    /// Sets a value directly in Redis (bypassing the API).
    /// </summary>
    protected async Task SetRawValueAsync(string key, string value)
    {
        await using var connection = await ConnectionMultiplexer.ConnectAsync(
            Redis.Container.GetConnectionString());
        var db = connection.GetDatabase();
        await db.StringSetAsync($"{KeyPrefix}{key}", value);
    }
}
