using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Example.NestedDataSources.DataGenerators;

public class MyWebFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    [ClassDataSource<PostgreWrapper>(Shared = SharedType.PerTestSession)]
    public required PostgreWrapper Postgre { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IPostgreClient>(new PostgreClient(Postgre.Container.GetConnectionString()));
        });
    }

    public Task InitializeAsync()
    {

        // This is a no-op, but we need to call Server to ensure the server is initialized.
        _ = Server;

        return Task.CompletedTask;
    }
}
