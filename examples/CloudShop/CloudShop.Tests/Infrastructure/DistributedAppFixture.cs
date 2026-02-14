using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using TUnit.Core.Interfaces;

namespace CloudShop.Tests.Infrastructure;

/// <summary>
/// Root fixture that starts the entire Aspire distributed application.
/// Shared across all tests in the session - the app is started once and reused.
/// </summary>
public class DistributedAppFixture : IAsyncInitializer, IAsyncDisposable
{
    private DistributedApplication? _app;

    public DistributedApplication App => _app ?? throw new InvalidOperationException("App not initialized");

    public async Task InitializeAsync()
    {
        // Allow HTTP transport so DCP doesn't require trusted dev certificates.
        // This is necessary in CI/test environments where certificates may not be trusted.
        Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CloudShop_AppHost>();

        _app = await builder.BuildAsync();

        await _app.StartAsync();

        // The AppHost defines WaitFor dependencies:
        //   apiservice waits for postgres, redis, rabbitmq
        //   worker waits for postgres, rabbitmq, apiservice
        // So waiting for the leaf services ensures all infrastructure is ready too.
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        await _app.ResourceNotifications.WaitForResourceAsync("apiservice", KnownResourceStates.Running, cts.Token);
        await _app.ResourceNotifications.WaitForResourceAsync("worker", KnownResourceStates.Running, cts.Token);
    }

    public HttpClient CreateHttpClient(string resourceName)
        => App.CreateHttpClient(resourceName);

    public async Task<string> GetConnectionStringAsync(string resourceName)
        => await App.GetConnectionStringAsync(resourceName)
           ?? throw new InvalidOperationException($"No connection string for '{resourceName}'");

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
