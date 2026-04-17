using Microsoft.Extensions.Http;

namespace TUnit.AspNetCore.Http;

/// <summary>
/// Prepends <see cref="ActivityPropagationHandler"/> and <see cref="TUnitTestIdHandler"/>
/// to every <see cref="System.Net.Http.IHttpClientFactory"/> handler pipeline built in the SUT.
/// Ensures outbound HTTP calls made via <c>AddHttpClient&lt;T&gt;()</c>, named, or typed clients
/// carry the current test's <c>traceparent</c>, <c>baggage</c>, and <c>X-TUnit-TestId</c> headers.
/// </summary>
internal sealed class TUnitHttpClientFilter : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) =>
        builder =>
        {
            next(builder);
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
}
