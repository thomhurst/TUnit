using Microsoft.AspNetCore.Hosting;
using TUnit.AspNetCore;
using TUnit.AspNetCore.Logging;
using TUnit.Core.Interfaces;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Shared web application factory for integration tests.
/// Overrides <see cref="ConfigureWebHost"/> to register <see cref="CorrelatedTUnitLoggingExtensions.AddCorrelatedTUnitLogging"/>
/// because the base class registers it in <c>CreateHostBuilder()</c>, which returns <c>null</c> for
/// minimal API apps (top-level statements). This is a known gap in <c>TestWebApplicationFactory</c>
/// — any minimal API host must register correlated logging via <c>ConfigureWebHost</c> instead.
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
