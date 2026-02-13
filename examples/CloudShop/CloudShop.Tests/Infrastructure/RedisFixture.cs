using StackExchange.Redis;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace CloudShop.Tests.Infrastructure;

/// <summary>
/// Provides direct Redis access for cache verification tests.
/// Nested dependency: injects DistributedAppFixture to get the connection string.
/// </summary>
public class RedisFixture : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DistributedAppFixture>(Shared = SharedType.PerTestSession)]
    public required DistributedAppFixture App { get; init; }

    public IConnectionMultiplexer Connection { get; private set; } = null!;
    public IDatabase Database => Connection.GetDatabase();

    public async Task InitializeAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("redis");
        Connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (Connection is not null)
            await Connection.DisposeAsync();
    }
}
