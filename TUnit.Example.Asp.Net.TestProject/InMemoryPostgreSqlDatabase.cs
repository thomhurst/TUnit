using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject;

public class InMemoryPostgreSqlDatabase : IAsyncInitializer, IAsyncDisposable
{
    public PostgreSqlContainer PostgreSqlContainer { get; } = new PostgreSqlBuilder()
        .WithUsername("User")
        .WithPassword("Password")
        .WithDatabase("TestDatabase")
        .Build();

    public async Task InitializeAsync() => await PostgreSqlContainer.StartAsync();

    public async ValueTask DisposeAsync() => await PostgreSqlContainer.DisposeAsync();
}
