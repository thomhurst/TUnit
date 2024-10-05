using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace ConsoleApp1;

public class MyFactory
    : WebApplicationFactory<Webapplication1.Program>,
        IAsyncInitializer,
        IAsyncDisposable
{
    public PostgreSqlContainer PostgresSqlContainer { get; } = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        // stick a breakpoint here - will be hit 2nd
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
        await base.DisposeAsync();
        await PostgresSqlContainer.DisposeAsync();
    }
}
