using System.Net;
using System.Text;

namespace TUnit.Mocks.Http;

/// <summary>
/// Builds an HTTP response for a matched request.
/// </summary>
public sealed class ResponseBuilder
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private Func<HttpContent>? _contentFactory;
    private readonly Dictionary<string, string> _headers = new();
    private TimeSpan? _delay;
    private Func<HttpRequestMessage, HttpResponseMessage>? _factory;

    /// <summary>Set the response status code.</summary>
    public ResponseBuilder WithStatus(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
        return this;
    }

    /// <summary>Set JSON response content. Serializes the object to JSON.</summary>
    public ResponseBuilder WithJsonContent(string json)
    {
        _contentFactory = () => new StringContent(json, Encoding.UTF8, "application/json");
        return this;
    }

    /// <summary>Set string response content.</summary>
    public ResponseBuilder WithStringContent(string content, string mediaType = "text/plain")
    {
        _contentFactory = () => new StringContent(content, Encoding.UTF8, mediaType);
        return this;
    }

    /// <summary>Set a response header.</summary>
    public ResponseBuilder WithHeader(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    /// <summary>Add a delay before returning the response (for timeout testing).</summary>
    public ResponseBuilder WithDelay(TimeSpan delay)
    {
        _delay = delay;
        return this;
    }

    /// <summary>Use a factory function to dynamically build the response from the request.</summary>
    public ResponseBuilder WithFactory(Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
        _factory = factory;
        return this;
    }

    internal TimeSpan? Delay => _delay;

    internal HttpResponseMessage Build(HttpRequestMessage request)
    {
        if (_factory != null)
            return _factory(request);

        var response = new HttpResponseMessage(_statusCode)
        {
            RequestMessage = request
        };

        if (_contentFactory != null)
            response.Content = _contentFactory();

        foreach (var kvp in _headers)
            response.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);

        return response;
    }
}
