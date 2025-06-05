using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace TUnit.Example.NestedDataSources.DataGenerators;

public class PostgreWrapper : IAsyncInitializer, IAsyncDisposable
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder().Build();

    public Task InitializeAsync() => Container.StartAsync();

    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
