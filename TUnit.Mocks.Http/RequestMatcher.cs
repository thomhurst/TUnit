using System.Text.RegularExpressions;

namespace TUnit.Mocks.Http;

/// <summary>
/// Fluent builder for matching HTTP requests.
/// </summary>
public sealed class RequestMatcher
{
    private HttpMethod? _method;
    private string? _exactPath;
    private string? _pathPrefix;
    private Regex? _pathPattern;
    private readonly Dictionary<string, string> _requiredHeaders = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _requiredHeaderNames = new();
    private Func<HttpRequestMessage, bool>? _customPredicate;
    private string? _bodyContains;

    /// <summary>Match requests with the specified HTTP method.</summary>
    public RequestMatcher Method(HttpMethod method)
    {
        _method = method;
        return this;
    }

    /// <summary>Match requests to the exact path.</summary>
    public RequestMatcher Path(string path)
    {
        _exactPath = path;
        return this;
    }

    /// <summary>Match requests whose path starts with the specified prefix.</summary>
    public RequestMatcher PathStartsWith(string prefix)
    {
        _pathPrefix = prefix;
        return this;
    }

    /// <summary>Match requests whose path matches the regex pattern.</summary>
    public RequestMatcher PathMatches(string pattern)
    {
        _pathPattern = new Regex(pattern, RegexOptions.Compiled);
        return this;
    }

    /// <summary>Match requests that have the specified header with the specified value.</summary>
    public RequestMatcher Header(string name, string value)
    {
        _requiredHeaders[name] = value;
        return this;
    }

    /// <summary>Match requests that have the specified header (any value).</summary>
    public RequestMatcher HasHeader(string name)
    {
        _requiredHeaderNames.Add(name);
        return this;
    }

    /// <summary>Match requests whose body contains the specified text.</summary>
    public RequestMatcher BodyContains(string text)
    {
        _bodyContains = text;
        return this;
    }

    /// <summary>Match requests using a custom predicate.</summary>
    public RequestMatcher Matching(Func<HttpRequestMessage, bool> predicate)
    {
        _customPredicate = predicate;
        return this;
    }

    internal bool Matches(HttpRequestMessage request, string? bodyContent)
    {
        if (_method != null && request.Method != _method) return false;

        var path = request.RequestUri?.PathAndQuery ?? request.RequestUri?.ToString() ?? "";

        if (!MatchesPath(path, request.RequestUri))
            return false;

        foreach (var kvp in _requiredHeaders)
        {
            if (!TryGetHeaderValue(request, kvp.Key, out var values) ||
                !values.Any(v => string.Equals(v, kvp.Value, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        foreach (var headerName in _requiredHeaderNames)
        {
            if (!TryGetHeaderValue(request, headerName, out _))
                return false;
        }

        if (_bodyContains != null && (bodyContent == null || !bodyContent.Contains(_bodyContains)))
            return false;

        if (_customPredicate != null && !_customPredicate(request))
            return false;

        return true;
    }

    internal bool Matches(CapturedRequest captured)
    {
        if (_method != null && captured.Method != _method) return false;

        var path = captured.RequestUri?.PathAndQuery ?? captured.RequestUri?.ToString() ?? "";

        if (!MatchesPath(path, captured.RequestUri))
            return false;

        foreach (var kvp in _requiredHeaders)
        {
            if (!TryGetCapturedHeaderValue(captured.Headers, kvp.Key, out var values) ||
                !values.Any(v => string.Equals(v, kvp.Value, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        foreach (var headerName in _requiredHeaderNames)
        {
            if (!TryGetCapturedHeaderValue(captured.Headers, headerName, out _))
                return false;
        }

        if (_bodyContains != null && (captured.Body == null || !captured.Body.Contains(_bodyContains)))
            return false;

        // Custom predicate requires HttpRequestMessage — skip if not available
        // (custom predicates are checked during SendAsync via the other Matches overload)

        return true;
    }

    private bool MatchesPath(string path, Uri? requestUri)
    {
        if (_exactPath != null && !string.Equals(path, _exactPath, StringComparison.OrdinalIgnoreCase))
        {
            var absUri = requestUri?.ToString() ?? "";
            if (!string.Equals(absUri, _exactPath, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (_pathPrefix != null && !path.StartsWith(_pathPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        if (_pathPattern != null && !_pathPattern.IsMatch(path))
            return false;

        return true;
    }

    private static bool TryGetHeaderValue(HttpRequestMessage request, string name, out IEnumerable<string> values)
    {
        if (request.Headers.TryGetValues(name, out values!))
            return true;
        if (request.Content != null && request.Content.Headers.TryGetValues(name, out values!))
            return true;
        values = Enumerable.Empty<string>();
        return false;
    }

    private static bool TryGetCapturedHeaderValue(IReadOnlyDictionary<string, IEnumerable<string>> headers, string name, out IEnumerable<string> values)
    {
        if (headers.TryGetValue(name, out values!))
            return true;
        values = Enumerable.Empty<string>();
        return false;
    }

    internal string Describe()
    {
        var parts = new List<string>();
        if (_method != null) parts.Add(_method.Method);
        if (_exactPath != null) parts.Add(_exactPath);
        if (_pathPrefix != null) parts.Add($"{_pathPrefix}*");
        if (_pathPattern != null) parts.Add($"~/{_pathPattern}/");
        if (_requiredHeaders.Count > 0) parts.Add($"headers:{string.Join(",", _requiredHeaders.Keys)}");
        if (_bodyContains != null) parts.Add($"body contains \"{_bodyContains}\"");
        return parts.Count > 0 ? string.Join(" ", parts) : "*";
    }
}
