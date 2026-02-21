using System.Collections.Concurrent;
using System.Net;
using TUnit.Mock.Exceptions;

namespace TUnit.Mock.Http;

/// <summary>
/// A mock HTTP message handler for testing HTTP client interactions.
/// Provides fluent request matching and response configuration.
/// </summary>
public sealed class MockHttpHandler : HttpMessageHandler
{
    private readonly List<RequestSetup> _setups = new();
    private readonly ConcurrentQueue<CapturedRequest> _requests = new();
    private HttpStatusCode _defaultStatusCode = HttpStatusCode.NotFound;
    private bool _throwOnUnmatched;

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> using this handler.
    /// </summary>
    public HttpClient CreateClient() => new(this);

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> with a base address using this handler.
    /// </summary>
    public HttpClient CreateClient(string baseAddress) => new(this)
    {
        BaseAddress = new Uri(baseAddress)
    };

    /// <summary>
    /// Set the default status code for unmatched requests. Default is 404 NotFound.
    /// </summary>
    public MockHttpHandler WithDefaultStatus(HttpStatusCode statusCode)
    {
        _defaultStatusCode = statusCode;
        return this;
    }

    /// <summary>
    /// Throw an exception for unmatched requests instead of returning a default response.
    /// </summary>
    public MockHttpHandler ThrowOnUnmatched()
    {
        _throwOnUnmatched = true;
        return this;
    }

    /// <summary>
    /// Configure a request-response setup using a fluent request matcher.
    /// </summary>
    public RequestSetup OnRequest(Action<RequestMatcher> configure)
    {
        var matcher = new RequestMatcher();
        configure(matcher);
        var setup = new RequestSetup(matcher);
        _setups.Add(setup);
        return setup;
    }

    /// <summary>
    /// Configure a setup matching any request.
    /// </summary>
    public RequestSetup OnAnyRequest()
    {
        return OnRequest(_ => { });
    }

    /// <summary>
    /// Configure a setup matching GET requests to the specified path.
    /// </summary>
    public RequestSetup OnGet(string path)
    {
        return OnRequest(r => r.Method(HttpMethod.Get).Path(path));
    }

    /// <summary>
    /// Configure a setup matching POST requests to the specified path.
    /// </summary>
    public RequestSetup OnPost(string path)
    {
        return OnRequest(r => r.Method(HttpMethod.Post).Path(path));
    }

    /// <summary>
    /// Configure a setup matching PUT requests to the specified path.
    /// </summary>
    public RequestSetup OnPut(string path)
    {
        return OnRequest(r => r.Method(HttpMethod.Put).Path(path));
    }

    /// <summary>
    /// Configure a setup matching DELETE requests to the specified path.
    /// </summary>
    public RequestSetup OnDelete(string path)
    {
        return OnRequest(r => r.Method(HttpMethod.Delete).Path(path));
    }

    /// <summary>All captured requests in order.</summary>
    public IReadOnlyList<CapturedRequest> Requests
    {
        get
        {
            var list = new List<CapturedRequest>();
            foreach (var r in _requests) list.Add(r);
            return list;
        }
    }

    /// <summary>Requests that did not match any setup.</summary>
    public IReadOnlyList<CapturedRequest> UnmatchedRequests
        => Requests.Where(r => !r.Matched).ToList();

    /// <summary>
    /// Verify a matching request was made the expected number of times.
    /// </summary>
    public void Verify(Action<RequestMatcher> configure, Times times)
    {
        var matcher = new RequestMatcher();
        configure(matcher);

        var matching = 0;
        foreach (var request in _requests)
        {
            if (matcher.Matches(
                new HttpRequestMessage(request.Method, request.RequestUri),
                request.Body))
            {
                matching++;
            }
        }

        if (!times.Matches(matching))
        {
            throw new MockVerificationException(
                matcher.Describe(),
                times,
                matching,
                Requests.Select(r => r.ToString()).ToList(),
                null);
        }
    }

    /// <summary>
    /// Verify no unexpected requests were made (all requests matched a setup).
    /// </summary>
    public void VerifyNoUnmatchedRequests()
    {
        var unmatched = UnmatchedRequests;
        if (unmatched.Count > 0)
        {
            throw new MockVerificationException(
                "no unmatched requests",
                Times.Never,
                unmatched.Count,
                unmatched.Select(r => r.ToString()).ToList(),
                null);
        }
    }

    /// <summary>Clears all setups and captured requests.</summary>
    public void Reset()
    {
        _setups.Clear();
        while (_requests.TryDequeue(out _)) { }
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var bodyContent = request.Content != null
            ? await request.Content.ReadAsStringAsync(
#if NET8_0_OR_GREATER
                cancellationToken
#endif
            ).ConfigureAwait(false)
            : null;

        var captured = new CapturedRequest(request, bodyContent);
        _requests.Enqueue(captured);

        foreach (var setup in _setups)
        {
            if (setup.TryMatch(request, bodyContent))
            {
                captured.Matched = true;
                var response = setup.GetNextResponse();
                if (response != null)
                {
                    if (response.Delay.HasValue)
                        await Task.Delay(response.Delay.Value, cancellationToken).ConfigureAwait(false);
                    return response.Build(request);
                }
            }
        }

        if (_throwOnUnmatched)
        {
            throw new HttpRequestException(
                $"No matching setup for {request.Method} {request.RequestUri}. " +
                $"Configure a setup using OnRequest() or use WithDefaultStatus() instead of ThrowOnUnmatched().");
        }

        return new HttpResponseMessage(_defaultStatusCode) { RequestMessage = request };
    }
}
