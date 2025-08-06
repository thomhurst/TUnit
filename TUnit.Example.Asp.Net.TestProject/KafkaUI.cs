using System.Diagnostics.CodeAnalysis;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject;

public class KafkaUI : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DockerNetwork>(Shared = SharedType.PerTestSession)]
    public required DockerNetwork DockerNetwork { get; init; }

    [ClassDataSource<InMemoryKafka>(Shared = SharedType.PerTestSession)]
    public required InMemoryKafka Kafka { get; init; }

    [field: AllowNull, MaybeNull]
    public IContainer Container => field ??= new ContainerBuilder()
        .WithNetwork(DockerNetwork.Instance)
        .WithImage("provectuslabs/kafka-ui:latest")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(".*Started KafkaUiApplication.*"))
        .WithPortBinding(8080, 8080)
        .WithEnvironment(new Dictionary<string, string>
        {
            ["KAFKA_CLUSTERS_0_NAME"] = "tunit_asp_net_tests",
            ["KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS"] = $"{Kafka.Container.Name}:9093",
            ["DYNAMIC_CONFIG_ENABLED"] = "true",
        })
        .Build();

    public async Task InitializeAsync() => await Container.StartAsync();

    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
