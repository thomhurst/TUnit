using Microsoft.AspNetCore.Hosting;
using TUnit.AspNetCore;
using TUnit.AspNetCore.Logging;
using TUnit.Core.Interfaces;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Shared web application factory for integration tests.
/// Extends TestWebApplicationFactory which automatically registers AddCorrelatedTUnitLogging()
/// (and therefore the HttpContextTestContextResolver) so that server-side logs are correlated
/// to the correct test context via the resolver mechanism.
/// </summary>
public class TestWebAppFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        // Eagerly start the server to catch configuration errors early
        _ = Server;
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // For minimal API apps, CreateHostBuilder() returns null so the base class's
        // AddCorrelatedTUnitLogging() in CreateHostBuilder is never called.
        // Register it here via ConfigureWebHost which IS called for minimal API apps.
        builder.ConfigureServices(services =>
        {
            services.AddCorrelatedTUnitLogging();
        });
    }
}
