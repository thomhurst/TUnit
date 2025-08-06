using System.Diagnostics.CodeAnalysis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject;

public class InMemoryRedis : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DockerNetwork>(Shared = SharedType.PerTestSession)]
    public required DockerNetwork DockerNetwork { get; init; }

    [field: AllowNull, MaybeNull]
    public RedisContainer Container => field ??= new RedisBuilder()
        .WithNetwork(DockerNetwork.Instance)
        .Build();

    public async Task InitializeAsync() => await Container.StartAsync();

    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
