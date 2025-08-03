using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject;

public class InMemoryRedis : IAsyncInitializer, IAsyncDisposable
{
    public RedisContainer PostgreSqlContainer { get; } = new RedisBuilder().Build();

    public async Task InitializeAsync() => await PostgreSqlContainer.StartAsync();

    public async ValueTask DisposeAsync() => await PostgreSqlContainer.DisposeAsync();
}
