using System.Text.RegularExpressions;

namespace TUnit.Assertions.AssertConditions;

/// <summary>
/// Matches a <see langword="string" /> against a pattern.
/// </summary>
public abstract class StringMatcher
{
    /// <summary>
    /// Matches the <paramref name="value" /> against the given match pattern.
    /// </summary>
    /// <param name="value">The value to match against the given pattern.</param>
    internal abstract bool Matches(string? value);

    /// <summary>
    /// Implicitly converts the <see langword="string" /> to a <see cref="Wildcard" /> pattern.<br />
    /// Supports * to match zero or more characters and ? to match exactly one character.
    /// </summary>
    public static implicit operator StringMatcher(string pattern) => AsWildcard(pattern);

    /// <summary>
    ///     A wildcard match.<br />
    ///     Supports * to match zero or more characters and ? to match exactly one character.
    /// </summary>
    public static WildcardMatch AsWildcard(string pattern)
        => new WildcardMatch(pattern);

    /// <summary>
    ///     A <see cref="Regex"> match.
    /// </summary>
    public static RegexMatch AsRegex(string pattern)
        => new RegexMatch(pattern);

    public sealed class WildcardMatch : StringMatcher
    {
        private readonly string _originalPattern;
        private readonly RegexMatch _regexMatch;

        internal WildcardMatch(string pattern)
        {
            _originalPattern = pattern;
            _regexMatch = new RegexMatch(WildcardToRegularExpression(pattern));
        }

        /// <inheritdoc cref="Match.Matches(string, bool)" />
        internal override bool Matches(string? value)
        {
            return _regexMatch.Matches(value);
        }

        /// <summary>
        /// Specifies if the match should be performed case-sensitive or not.
        /// </summary>
        public WildcardMatch IgnoringCase(bool ignoreCase = true)
        {
            _regexMatch.IgnoringCase(ignoreCase);
            return this;
        }

        /// <inheritdoc cref="object.ToString()" />
        public override string ToString()
            => _originalPattern;

        private static string WildcardToRegularExpression(string value)
        {
            string regex = Regex.Escape(value)
                .Replace("\\?", ".")
                .Replace("\\*", ".*");
            return $"^{regex}$";
        }
    }

    public sealed class RegexMatch : StringMatcher
    {
        private readonly string _pattern;
        private bool _ignoreCase;

        internal RegexMatch(string pattern)
        {
            _pattern = pattern;
        }

        /// <inheritdoc cref="Match.Matches(string, bool)" />
        internal override bool Matches(string? value)
        {
            if (value == null)
            {
                return false;
            }

            RegexOptions options = RegexOptions.Multiline;
            if (_ignoreCase)
            {
                options |= RegexOptions.IgnoreCase;
            }

            return Regex.IsMatch(value, _pattern, options);
        }

        /// <summary>
        /// Specifies if the match should be performed case-sensitive or not.
        /// </summary>
        public RegexMatch IgnoringCase(bool ignoreCase = true)
        {
            _ignoreCase = ignoreCase;
            return this;
        }

        /// <inheritdoc cref="object.ToString()" />
        public override string ToString()
            => _pattern;
    }
}