using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject;

public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    private int _configuredWebHostCalled;

    public int ConfiguredWebHostCalled => _configuredWebHostCalled;

    public Task InitializeAsync()
    {
        _ = Server;

        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Interlocked.Increment(ref _configuredWebHostCalled);
        
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var testName = TestContext.Current!.TestDetails.TestName;
            
            var connectionString = testName + "-DummyConnectionString";
            
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", connectionString }
            });
        });
    }
}