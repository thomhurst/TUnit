using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.Extensions.Http;

namespace TUnit.AspNetCore.Http;

/// <summary>
/// Prepends <see cref="ActivityPropagationHandler"/> and <see cref="TUnitTestIdHandler"/>
/// to every <see cref="System.Net.Http.IHttpClientFactory"/> handler pipeline built in the SUT.
/// Ensures outbound HTTP calls made via <c>AddHttpClient&lt;T&gt;()</c>, named, or typed clients
/// carry the current test's <c>traceparent</c>, <c>baggage</c>, and <c>X-TUnit-TestId</c> headers.
/// <para>
/// Both handler types must remain stateless and thread-safe: <see cref="System.Net.Http.IHttpClientFactory"/>
/// caches the built pipeline and shares the same handler instances across every request on a given
/// named client, including concurrent requests from parallel tests. Per-test correlation comes from
/// <see cref="TUnit.Core.TestContext.Current"/> and <see cref="System.Diagnostics.Activity.Current"/>,
/// which are async-local — do not add instance fields capturing per-request state to either handler.
/// </para>
/// </summary>
internal sealed class TUnitHttpClientFilter : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) =>
        builder =>
        {
            next(builder);
            // Insert at outermost positions so TUnit headers are emitted before any
            // SUT-registered handler can run. Order must stay ActivityPropagationHandler
            // first (writes traceparent/baggage) then TUnitTestIdHandler (writes X-TUnit-TestId).
            builder.AdditionalHandlers.Insert(0, new ActivityPropagationHandler());
            builder.AdditionalHandlers.Insert(1, new TUnitTestIdHandler());
        };

    /// <summary>
    /// Returns the TUnit propagation handlers followed by the caller-supplied handlers,
    /// in the order they should be passed to <c>WebApplicationFactory.CreateDefaultClient</c>.
    /// </summary>
    internal static DelegatingHandler[] PrependPropagationHandlers(DelegatingHandler[] handlers)
    {
        var all = new DelegatingHandler[handlers.Length + 2];
        all[0] = new ActivityPropagationHandler();
        all[1] = new TUnitTestIdHandler();
        Array.Copy(handlers, 0, all, 2, handlers.Length);
        return all;
    }

    /// <summary>
    /// Creates a client the way <c>WebApplicationFactory.CreateClient(WebApplicationFactoryClientOptions)</c>
    /// does, with the TUnit propagation handlers added. <paramref name="createDefaultClient"/> must be the
    /// factory's base <c>CreateDefaultClient(DelegatingHandler[])</c>, which also runs the factory's
    /// <c>ConfigureClient</c> override.
    /// </summary>
    internal static HttpClient CreateClient(
        Func<DelegatingHandler[], HttpClient> createDefaultClient,
        WebApplicationFactoryClientOptions options)
    {
        var client = createDefaultClient(CreateClientOptionsHandlers(options));
        // Read BaseAddress after the client is created: starting a Kestrel-backed factory
        // replaces ClientOptions.BaseAddress with the server's real address.
        client.BaseAddress = options.BaseAddress;
        return client;
    }

    /// <summary>
    /// Returns the handlers that <see cref="WebApplicationFactoryClientOptions"/> requests, followed by the
    /// TUnit propagation handlers: a <see cref="RedirectHandler"/> when
    /// <see cref="WebApplicationFactoryClientOptions.AllowAutoRedirect"/> is set, then a
    /// <see cref="CookieContainerHandler"/> when <see cref="WebApplicationFactoryClientOptions.HandleCookies"/>
    /// is set. Mirrors the internal <c>WebApplicationFactoryClientOptions.CreateHandlers</c> in
    /// Microsoft.AspNetCore.Mvc.Testing 8.0 to 10.0; update this if a later version adds a handler.
    /// </summary>
    /// <remarks>
    /// The propagation handlers sit inside <see cref="RedirectHandler"/> so that every redirect hop
    /// gets freshly injected headers, not only the first request.
    /// </remarks>
    internal static DelegatingHandler[] CreateClientOptionsHandlers(WebApplicationFactoryClientOptions options)
    {
        var count = 2 + (options.AllowAutoRedirect ? 1 : 0) + (options.HandleCookies ? 1 : 0);
        var all = new DelegatingHandler[count];
        var index = 0;

        if (options.AllowAutoRedirect)
        {
            all[index++] = new RedirectHandler(options.MaxAutomaticRedirections);
        }

        if (options.HandleCookies)
        {
            all[index++] = new CookieContainerHandler();
        }

        all[index++] = new ActivityPropagationHandler();
        all[index] = new TUnitTestIdHandler();

        return all;
    }
}
