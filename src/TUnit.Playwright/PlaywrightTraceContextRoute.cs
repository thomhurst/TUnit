using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TUnit.Playwright;

/// <summary>
/// Adds W3C trace context headers to requests sent to the origins under test only.
/// Context-wide <c>ExtraHTTPHeaders</c> would also reach third-party origins, where the
/// non-safelisted <c>traceparent</c> header triggers CORS preflights that usually fail.
/// </summary>
internal sealed class PlaywrightTraceContextRoute
{
    private readonly Regex _origins;
    private readonly KeyValuePair<string, string>[] _headers;

    private PlaywrightTraceContextRoute(Regex origins, KeyValuePair<string, string>[] headers)
    {
        _origins = origins;
        _headers = headers;
    }

    /// <summary>
    /// Captures the trace headers of the current <see cref="System.Diagnostics.Activity"/>.
    /// Returns <c>null</c> when propagation is disabled, there is no activity, or no origin
    /// is known. Without explicit <paramref name="origins"/>, the origin of
    /// <see cref="BrowserNewContextOptions.BaseURL"/> is used.
    /// </summary>
    public static PlaywrightTraceContextRoute? Create(bool propagate, IReadOnlyList<string>? origins, BrowserNewContextOptions options)
    {
#if NET
        if (!propagate || System.Diagnostics.Activity.Current is null)
        {
            return null;
        }

        var pattern = BuildOriginPattern(origins, options.BaseURL);
        if (pattern is null)
        {
            return null;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Telemetry.PlaywrightActivityPropagator.InjectInto(headers);
        return headers.Count == 0
            ? null
            : new PlaywrightTraceContextRoute(pattern, headers.ToArray());
#else
        return null;
#endif
    }

    public Task RegisterAsync(IBrowserContext context) => context.RouteAsync(_origins, HandleAsync);

    private Task HandleAsync(IRoute route)
    {
        var headers = new Dictionary<string, string>(route.Request.Headers, StringComparer.OrdinalIgnoreCase);
        foreach (var header in _headers)
        {
            // Headers set by the page or by ExtraHTTPHeaders win.
            if (!headers.ContainsKey(header.Key))
            {
                headers[header.Key] = header.Value;
            }
        }

        // Fallback rather than Continue, so any handler registered before this one still runs.
        return route.FallbackAsync(new RouteFallbackOptions { Headers = headers });
    }

    internal static Regex? BuildOriginPattern(IReadOnlyList<string>? origins, string? baseUrl)
    {
        if (origins is null)
        {
            return string.IsNullOrEmpty(baseUrl) ? null : BuildOriginPattern([baseUrl!], null);
        }

        if (origins.Count == 0)
        {
            return null;
        }

        var pattern = new StringBuilder("^(?:");
        for (var i = 0; i < origins.Count; i++)
        {
            if (i > 0)
            {
                pattern.Append('|');
            }

            pattern.Append(Regex.Escape(NormalizeOrigin(origins[i])));
        }

        // Match the origin exactly: "https://app.test" must not match "https://app.test.evil".
        pattern.Append(")(?:[/?#]|$)");
        return new Regex(pattern.ToString(), RegexOptions.IgnoreCase);
    }

    private static string NormalizeOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"Trace context origin '{origin}' must be an absolute http or https URL.", nameof(origin));
        }

        // Scheme, host and non-default port, as the browser writes request URLs.
        return uri.GetLeftPart(UriPartial.Authority);
    }
}
