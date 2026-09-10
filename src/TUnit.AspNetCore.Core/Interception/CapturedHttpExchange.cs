using System.Net;

namespace TUnit.AspNetCore.Interception;

/// <summary>
/// Represents a captured HTTP request/response exchange.
/// </summary>
public sealed class CapturedHttpExchange
{
    /// <summary>
    /// Gets the unique identifier for this exchange.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when the request was received.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the duration of the request processing.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the captured request details.
    /// </summary>
    public required CapturedRequest Request { get; init; }

    /// <summary>
    /// Gets the captured response details.
    /// </summary>
    public required CapturedResponse Response { get; init; }
}

/// <summary>
/// Represents a captured HTTP request.
/// </summary>
public sealed class CapturedRequest
{
    /// <summary>
    /// Gets the HTTP method (GET, POST, etc.).
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Gets the request path.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the query string (without the leading '?').
    /// </summary>
    public string? QueryString { get; init; }

    /// <summary>
    /// Gets the full URL (path + query string).
    /// </summary>
    public string Url => string.IsNullOrEmpty(QueryString) ? Path : $"{Path}?{QueryString}";

    /// <summary>
    /// Gets the request headers.
    /// </summary>
    public IReadOnlyDictionary<string, string?[]> Headers { get; init; } = new Dictionary<string, string?[]>();

    /// <summary>
    /// Gets the Content-Type header value, if present.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the request body as a string, if captured.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Gets the request body length in bytes.
    /// </summary>
    public long? ContentLength { get; init; }
}

/// <summary>
/// Represents a captured HTTP response.
/// </summary>
public sealed class CapturedResponse
{
    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public required HttpStatusCode StatusCode { get; init; }

    /// <summary>
    /// Gets the status code as an integer.
    /// </summary>
    public int StatusCodeValue => (int)StatusCode;

    /// <summary>
    /// Gets the response headers.
    /// </summary>
    public IReadOnlyDictionary<string, string?[]> Headers { get; init; } = new Dictionary<string, string?[]>();

    /// <summary>
    /// Gets the Content-Type header value, if present.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the response body as a string, if captured.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Gets the response body length in bytes.
    /// </summary>
    public long? ContentLength { get; init; }
}
