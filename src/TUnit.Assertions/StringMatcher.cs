using System.Text.RegularExpressions;

namespace TUnit.Assertions;

/// <summary>
/// Builder for creating string pattern matchers with various options.
/// Supports wildcards (*?) and regular expressions with case sensitivity control.
/// </summary>
public class StringMatcher
{
    private readonly string _pattern;
    private readonly MatcherType _type;
    private bool _ignoreCase;

    private enum MatcherType
    {
        Wildcard,
        Regex
    }

    private StringMatcher(string pattern, MatcherType type)
    {
        _pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        _type = type;
    }

    /// <summary>
    /// Creates a wildcard pattern matcher (* matches any characters, ? matches single character).
    /// By default, matching is case-sensitive.
    /// </summary>
    public static StringMatcher AsWildcard(string pattern) => new(pattern, MatcherType.Wildcard);

    /// <summary>
    /// Creates a regular expression pattern matcher.
    /// By default, matching is case-sensitive.
    /// </summary>
    public static StringMatcher AsRegex(string pattern) => new(pattern, MatcherType.Regex);

    /// <summary>
    /// Makes this matcher case-insensitive.
    /// </summary>
    public StringMatcher IgnoringCase()
    {
        _ignoreCase = true;
        return this;
    }

    /// <summary>
    /// Tests if the input string matches this pattern.
    /// </summary>
    public bool IsMatch(string? input)
    {
        if (input == null)
        {
            return false;
        }

        return _type switch
        {
            MatcherType.Wildcard => MatchWildcard(input, _pattern, _ignoreCase),
            MatcherType.Regex => MatchRegex(input, _pattern, _ignoreCase),
            _ => throw new InvalidOperationException($"Unknown matcher type: {_type}")
        };
    }

    private static bool MatchWildcard(string input, string pattern, bool ignoreCase)
    {
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // Convert wildcard pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        return Regex.IsMatch(input, regexPattern, options);
    }

    private static bool MatchRegex(string input, string pattern, bool ignoreCase)
    {
        var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        return Regex.IsMatch(input, pattern, options);
    }

    public override string ToString()
    {
        var typeName = _type == MatcherType.Wildcard ? "wildcard" : "regex";
        var caseInfo = _ignoreCase ? " (case-insensitive)" : "";
        return $"{typeName} pattern \"{_pattern}\"{caseInfo}";
    }

    internal string Pattern => _pattern;
    internal bool IgnoreCase => _ignoreCase;
    internal bool IsWildcard => _type == MatcherType.Wildcard;
    internal bool IsRegex => _type == MatcherType.Regex;
}
