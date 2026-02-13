using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
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
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CloudShop_AppHost>();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
        });

        _app = await builder.BuildAsync();

        await _app.StartAsync();

        // Wait for all resources to become healthy with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _app.ResourceNotifications.WaitForResourceAsync("postgresdb", KnownResourceStates.Running, cts.Token);
        await _app.ResourceNotifications.WaitForResourceAsync("redis", KnownResourceStates.Running, cts.Token);
        await _app.ResourceNotifications.WaitForResourceAsync("rabbitmq", KnownResourceStates.Running, cts.Token);
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
