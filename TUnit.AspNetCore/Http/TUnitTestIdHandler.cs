using TUnit.Core;

namespace TUnit.AspNetCore;

/// <summary>
/// A delegating handler that propagates the current test context ID via an HTTP header.
/// Use this with <see cref="TUnit.AspNetCore.Logging.TUnitTestContextMiddleware"/> to correlate
/// server-side logs with the originating test when using a shared <c>WebApplicationFactory</c>.
/// </summary>
public class TUnitTestIdHandler : DelegatingHandler
{
    /// <summary>
    /// The HTTP header name used to propagate the test context ID.
    /// </summary>
    public const string HeaderName = "X-TUnit-TestId";

    /// <summary>
    /// Creates a new <see cref="TUnitTestIdHandler"/> with a default <see cref="HttpClientHandler"/> as the inner handler.
    /// </summary>
    public TUnitTestIdHandler() : base(new HttpClientHandler())
    {
    }

    /// <summary>
    /// Creates a new <see cref="TUnitTestIdHandler"/> with the specified inner handler.
    /// </summary>
    /// <param name="innerHandler">The inner handler to delegate to.</param>
    public TUnitTestIdHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (TestContext.Current is { } ctx)
        {
            request.Headers.TryAddWithoutValidation(HeaderName, ctx.Id);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
