using System.Collections.Concurrent;
using System.Net;

namespace TUnit.AspNetCore.Interception;

/// <summary>
/// Stores captured HTTP exchanges for test assertions.
/// Register as a singleton in the test service collection.
/// </summary>
public sealed class HttpExchangeCapture
{
    private readonly ConcurrentQueue<CapturedHttpExchange> _exchanges = new();

    /// <summary>
    /// Gets or sets whether to capture request bodies. Default is true.
    /// Disable for large payloads or binary content.
    /// </summary>
    public bool CaptureRequestBody { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to capture response bodies. Default is true.
    /// Disable for large payloads or binary content.
    /// </summary>
    public bool CaptureResponseBody { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum body size to capture in bytes. Default is 1MB.
    /// Bodies larger than this will be truncated.
    /// </summary>
    public int MaxBodySize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets all captured exchanges in order.
    /// </summary>
    public IReadOnlyList<CapturedHttpExchange> Exchanges => [.. _exchanges];

    /// <summary>
    /// Gets the number of captured exchanges.
    /// </summary>
    public int Count => _exchanges.Count;

    /// <summary>
    /// Gets the most recent exchange, or null if none captured.
    /// </summary>
    public CapturedHttpExchange? Last => _exchanges.LastOrDefault();

    /// <summary>
    /// Gets the first exchange, or null if none captured.
    /// </summary>
    public CapturedHttpExchange? First => _exchanges.FirstOrDefault();

    /// <summary>
    /// Adds a captured exchange to the store.
    /// </summary>
    internal void Add(CapturedHttpExchange exchange)
    {
        _exchanges.Enqueue(exchange);
    }

    /// <summary>
    /// Clears all captured exchanges.
    /// </summary>
    public void Clear()
    {
        while (_exchanges.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Gets exchanges matching the specified HTTP method.
    /// </summary>
    public IEnumerable<CapturedHttpExchange> ForMethod(string method) =>
        _exchanges.Where(e => e.Request.Method.Equals(method, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets exchanges matching the specified path (exact match).
    /// </summary>
    public IEnumerable<CapturedHttpExchange> ForPath(string path) =>
        _exchanges.Where(e => e.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets exchanges where the path starts with the specified prefix.
    /// </summary>
    public IEnumerable<CapturedHttpExchange> ForPathStartingWith(string prefix) =>
        _exchanges.Where(e => e.Request.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets exchanges matching the specified status code.
    /// </summary>
    public IEnumerable<CapturedHttpExchange> ForStatusCode(HttpStatusCode statusCode) =>
        _exchanges.Where(e => e.Response.StatusCode == statusCode);

    /// <summary>
    /// Gets exchanges matching the specified status code.
    /// </summary>
    public IEnumerable<CapturedHttpExchange> ForStatusCode(int statusCode) =>
        _exchanges.Where(e => e.Response.StatusCodeValue == statusCode);

    /// <summary>
    /// Gets exchanges matching the specified method and path.
    /// </summary>
    public IEnumerable<CapturedHttpExchange> For(string method, string path) =>
        _exchanges.Where(e =>
            e.Request.Method.Equals(method, StringComparison.OrdinalIgnoreCase) &&
            e.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets exchanges matching the predicate.
    /// </summary>
    public IEnumerable<CapturedHttpExchange> Where(Func<CapturedHttpExchange, bool> predicate) =>
        _exchanges.Where(predicate);

    /// <summary>
    /// Returns true if any exchange matches the predicate.
    /// </summary>
    public bool Any(Func<CapturedHttpExchange, bool> predicate) =>
        _exchanges.Any(predicate);

    /// <summary>
    /// Returns true if any exchange was captured for the given method and path.
    /// </summary>
    public bool Any(string method, string path) =>
        For(method, path).Any();
}
