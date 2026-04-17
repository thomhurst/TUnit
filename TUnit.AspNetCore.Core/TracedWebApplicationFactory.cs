using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using TUnit.AspNetCore.Http;

namespace TUnit.AspNetCore;

/// <summary>
/// Wrapper around <see cref="WebApplicationFactory{TEntryPoint}"/> that automatically injects
/// <see cref="ActivityPropagationHandler"/> and <see cref="TUnitTestIdHandler"/> into all
/// created <see cref="HttpClient"/> instances.
/// <para>
/// HTTP requests made through clients created by this factory will:
/// <list type="bullet">
///   <item><description>Appear as spans in the HTML report's trace timeline</description></item>
///   <item><description>Propagate W3C <c>traceparent</c> headers for server-side span correlation</description></item>
///   <item><description>Propagate the current test's context ID for log correlation</description></item>
/// </list>
/// </para>
/// </summary>
/// <typeparam name="TEntryPoint">The entry point class of the web application.</typeparam>
public sealed class TracedWebApplicationFactory<TEntryPoint> : IAsyncDisposable, IDisposable
    where TEntryPoint : class
{
    private readonly WebApplicationFactory<TEntryPoint> _inner;

    public TracedWebApplicationFactory(WebApplicationFactory<TEntryPoint> inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Gets the <see cref="TestServer"/> instance.
    /// </summary>
    public TestServer Server => _inner.Server;

    /// <summary>
    /// Gets the application's <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider Services => _inner.Services;

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with activity tracing and test context propagation.
    /// </summary>
    public HttpClient CreateClient() =>
        _inner.CreateDefaultClient(TUnitHttpClientFilter.PrependPropagationHandlers([]));

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with the specified delegating handlers, plus
    /// activity tracing and test context propagation (prepended before custom handlers).
    /// </summary>
    public HttpClient CreateDefaultClient(params DelegatingHandler[] handlers)
    {
        return _inner.CreateDefaultClient(TUnitHttpClientFilter.PrependPropagationHandlers(handlers));
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with the specified base address and delegating handlers,
    /// plus activity tracing and test context propagation (prepended before custom handlers).
    /// </summary>
    public HttpClient CreateDefaultClient(Uri baseAddress, params DelegatingHandler[] handlers)
    {
        var client = CreateDefaultClient(handlers);
        client.BaseAddress = baseAddress;
        return client;
    }

    /// <summary>
    /// Gets the underlying <see cref="WebApplicationFactory{TEntryPoint}"/> for advanced scenarios
    /// that need direct access (e.g., calling <c>WithWebHostBuilder</c>).
    /// Clients created from the inner factory will NOT have automatic tracing.
    /// </summary>
    public WebApplicationFactory<TEntryPoint> Inner => _inner;

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _inner.DisposeAsync();

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();
}
