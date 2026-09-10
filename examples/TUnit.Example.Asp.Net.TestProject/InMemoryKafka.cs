using System.Diagnostics.CodeAnalysis;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject;

public class InMemoryKafka : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DockerNetwork>(Shared = SharedType.PerTestSession)]
    public required DockerNetwork DockerNetwork { get; init; }

    [field: AllowNull, MaybeNull]
    public KafkaContainer Container => field ??= new KafkaBuilder()
        .WithNetwork(DockerNetwork.Instance)
        .Build();

    public async Task InitializeAsync() => await Container.StartAsync();

    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
