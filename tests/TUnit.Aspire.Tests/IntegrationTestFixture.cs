using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Aspire fixture that starts the test AppHost (including the API service)
/// with OTLP telemetry collection enabled. Shared per test session so the
/// Aspire application is started once and reused across all integration tests.
/// </summary>
public class IntegrationTestFixture : AspireFixture<Projects.TUnit_Aspire_Tests_AppHost>
{
    protected override TimeSpan ResourceTimeout => TimeSpan.FromSeconds(120);

    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.AllRunning;

    protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
    {
        builder.Services.AddSingleton<HttpHandlerInvocationCounter>();
        builder.Services.AddTransient<CountingDelegatingHandler>();
        builder.Services.ConfigureHttpClientDefaults(http =>
            http.AddHttpMessageHandler<CountingDelegatingHandler>());
    }

    public int HttpHandlerInvocationCount
        => App.Services.GetRequiredService<HttpHandlerInvocationCounter>().Count;
}

internal sealed class HttpHandlerInvocationCounter
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Increment() => Interlocked.Increment(ref _count);
}

internal sealed class CountingDelegatingHandler(HttpHandlerInvocationCounter counter) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        counter.Increment();
        return base.SendAsync(request, cancellationToken);
    }
}
