using System.Diagnostics.CodeAnalysis;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject;

public class InMemoryPostgreSqlDatabase : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DockerNetwork>(Shared = SharedType.PerTestSession)]
    public required DockerNetwork DockerNetwork { get; init; }

    [field: AllowNull, MaybeNull]
    public PostgreSqlContainer Container => field ??= new PostgreSqlBuilder()
        .WithUsername("User")
        .WithPassword("Password")
        .WithDatabase("TestDatabase")
        .WithNetwork(DockerNetwork.Instance)
        .Build();

    public async Task InitializeAsync() => await Container.StartAsync();

    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
