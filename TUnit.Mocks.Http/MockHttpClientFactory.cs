namespace TUnit.Mocks.Http;

/// <summary>
/// A mock <see cref="IHttpClientFactory"/> backed by one or more <see cref="MockHttpHandler"/>s.
/// Each call to <see cref="CreateClient(string)"/> returns a fresh <see cref="HttpClient"/> that
/// does not dispose the underlying handler, so captured requests survive across <c>using</c> blocks.
/// </summary>
public sealed class MockHttpClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, MockHttpHandler> _named = new(StringComparer.Ordinal);

    /// <summary>The default handler used for clients with no name-specific handler configured.</summary>
    public MockHttpHandler Handler { get; }

    /// <summary>Creates a factory with a fresh default handler.</summary>
    public MockHttpClientFactory() : this(new MockHttpHandler())
    {
    }

    /// <summary>Creates a factory using the supplied default handler.</summary>
    public MockHttpClientFactory(MockHttpHandler handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// Register a dedicated handler for a named client. Subsequent <see cref="CreateClient(string)"/>
    /// calls with this name will use the supplied handler.
    /// </summary>
    public MockHttpClientFactory WithHandler(string name, MockHttpHandler handler)
    {
        _named[name] = handler;
        return this;
    }

    /// <summary>
    /// Gets the handler associated with the named client, falling back to <see cref="Handler"/>
    /// if no name-specific handler has been registered.
    /// </summary>
    public MockHttpHandler HandlerFor(string name)
        => _named.TryGetValue(name, out var h) ? h : Handler;

    /// <inheritdoc />
    public HttpClient CreateClient(string name)
        => new(HandlerFor(name), disposeHandler: false);
}
