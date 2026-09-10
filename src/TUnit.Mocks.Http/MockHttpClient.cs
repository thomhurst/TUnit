namespace TUnit.Mocks.Http;

/// <summary>
/// An <see cref="HttpClient"/> backed by a <see cref="MockHttpHandler"/>.
/// Use <see cref="Handler"/> to configure request setups and verify calls.
/// </summary>
public class MockHttpClient : HttpClient
{
    /// <summary>The underlying mock handler for configuring setups and verifications.</summary>
    public MockHttpHandler Handler { get; }

    /// <summary>Creates a new mock HTTP client with a fresh handler.</summary>
    public MockHttpClient() : this(new MockHttpHandler())
    {
    }

    /// <summary>Creates a new mock HTTP client with a fresh handler and the specified base address.</summary>
    public MockHttpClient(string baseAddress) : this(new MockHttpHandler())
    {
        BaseAddress = new Uri(baseAddress);
    }

    /// <summary>Creates a new mock HTTP client wrapping the specified handler.</summary>
    public MockHttpClient(MockHttpHandler handler) : base(handler)
    {
        Handler = handler;
    }
}
