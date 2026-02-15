using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Aspire;

namespace CloudShop.Tests.Infrastructure;

/// <summary>
/// Root fixture that starts the entire Aspire distributed application.
/// Shared across all tests in the session - the app is started once and reused.
/// </summary>
public class DistributedAppFixture : AspireFixture<Projects.CloudShop_AppHost>
{
    protected override TimeSpan ResourceTimeout => TimeSpan.FromMinutes(2);

    protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
    {
        // Allow HTTP transport so DCP doesn't require trusted dev certificates.
        // This is necessary in CI/test environments where certificates may not be trusted.
        Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
    }

    protected override async Task WaitForResourcesAsync(DistributedApplication app, CancellationToken cancellationToken)
    {
        // The AppHost defines WaitFor dependencies:
        //   apiservice waits for postgres, redis, rabbitmq
        //   worker waits for postgres, rabbitmq, apiservice
        // So waiting for the leaf services ensures all infrastructure is ready too.
        var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await notificationService.WaitForResourceAsync("apiservice", KnownResourceStates.Running, cancellationToken);
        await notificationService.WaitForResourceAsync("worker", KnownResourceStates.Running, cancellationToken);
    }

    public async Task<string> GetConnectionStringAsync(string resourceName)
        => await GetConnectionStringAsync(resourceName, CancellationToken.None)
           ?? throw new InvalidOperationException($"No connection string for '{resourceName}'");
}
