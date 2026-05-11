namespace TUnit.Mocks.Http;

/// <summary>
/// A mock <see cref="IHttpClientFactory"/>. Each <see cref="CreateClient(string)"/> call returns a
/// fresh <see cref="HttpClient"/> with handler disposal disabled, so captured requests on the
/// shared <see cref="MockHttpHandler"/> survive across <c>using</c> blocks.
/// </summary>
public sealed class MockHttpClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, MockHttpHandler> _named = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Uri> _baseAddresses = new(StringComparer.OrdinalIgnoreCase);
    private Uri? _defaultBaseAddress;

    public MockHttpHandler Handler { get; }

    public MockHttpClientFactory() : this(new MockHttpHandler())
    {
    }

    public MockHttpClientFactory(MockHttpHandler handler)
    {
        Handler = handler;
    }

    /// <summary>Register a dedicated handler for a named client.</summary>
    public MockHttpClientFactory WithHandler(string name, MockHttpHandler handler)
    {
        _named[name] = handler;
        return this;
    }

    /// <summary>Set the base address applied to every <see cref="CreateClient(string)"/> result that has no name-specific override.</summary>
    public MockHttpClientFactory WithBaseAddress(string baseAddress)
    {
        _defaultBaseAddress = new Uri(baseAddress);
        return this;
    }

    /// <summary>Set the base address applied to clients created with the specified name.</summary>
    public MockHttpClientFactory WithBaseAddress(string name, string baseAddress)
    {
        _baseAddresses[name] = new Uri(baseAddress);
        return this;
    }

    /// <summary>Gets the handler for the named client, falling back to <see cref="Handler"/>.</summary>
    public MockHttpHandler HandlerFor(string name)
        => _named.TryGetValue(name, out var h) ? h : Handler;

    /// <inheritdoc />
    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient(HandlerFor(name), disposeHandler: false);
        var baseAddress = _baseAddresses.TryGetValue(name, out var named) ? named : _defaultBaseAddress;
        if (baseAddress is not null)
        {
            client.BaseAddress = baseAddress;
        }
        return client;
    }
}
