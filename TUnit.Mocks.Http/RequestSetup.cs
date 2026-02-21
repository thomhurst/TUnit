using System.Net;

namespace TUnit.Mocks.Http;

/// <summary>
/// A configured request-response pair in the mock handler.
/// </summary>
public sealed class RequestSetup
{
    private readonly List<ResponseBuilder> _responses = new();
    private readonly System.Threading.Lock _responseLock = new();
    private int _responseIndex;

    internal RequestMatcher Matcher { get; }
    internal int MatchCount { get; private set; }

    internal RequestSetup(RequestMatcher matcher)
    {
        Matcher = matcher;
    }

    /// <summary>Configure a response for matched requests.</summary>
    public ResponseBuilder Respond(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var builder = new ResponseBuilder().WithStatus(statusCode);
        _responses.Add(builder);
        return builder;
    }

    /// <summary>Configure a JSON response.</summary>
    public ResponseBuilder RespondWithJson(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var builder = new ResponseBuilder().WithStatus(statusCode).WithJsonContent(json);
        _responses.Add(builder);
        return builder;
    }

    /// <summary>Configure a string response.</summary>
    public ResponseBuilder RespondWithString(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var builder = new ResponseBuilder().WithStatus(statusCode).WithStringContent(content);
        _responses.Add(builder);
        return builder;
    }

    /// <summary>Chain another response for subsequent matched requests.</summary>
    public RequestSetup Then()
    {
        return this;
    }

    /// <summary>Configure the matched request to throw an exception.</summary>
    public void Throws(Exception exception)
    {
        var builder = new ResponseBuilder().WithFactory(_ => throw exception);
        _responses.Add(builder);
    }

    /// <summary>Configure the matched request to throw an HttpRequestException.</summary>
    public void Throws(string message)
    {
        Throws(new HttpRequestException(message));
    }

    internal bool TryMatch(HttpRequestMessage request, string? bodyContent)
    {
        return Matcher.Matches(request, bodyContent);
    }

    internal ResponseBuilder? GetNextResponse()
    {
        lock (_responseLock)
        {
            if (_responses.Count == 0) return null;
            MatchCount++;
            var index = _responseIndex < _responses.Count ? _responseIndex : _responses.Count - 1;
            if (_responseIndex < _responses.Count) _responseIndex++;
            return _responses[index];
        }
    }
}
