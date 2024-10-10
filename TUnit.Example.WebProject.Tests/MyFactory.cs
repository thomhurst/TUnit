using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace TUnit.Example.WebProject.Tests;

public class MyFactory
    : WebApplicationFactory<Program>,
        IAsyncInitializer,
        IAsyncDisposable
{
    public PostgreSqlContainer PostgresSqlContainer { get; } = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await PostgresSqlContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services => { });
        // stick a breakpoint here - will be hit 1st
        base.ConfigureWebHost(builder);
    }

    public new async ValueTask DisposeAsync()
    {
        await PostgresSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}

