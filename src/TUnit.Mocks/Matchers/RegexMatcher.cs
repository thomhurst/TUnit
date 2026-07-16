using System.Text.RegularExpressions;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Matches string arguments against a regular expression pattern.
/// </summary>
internal sealed class RegexMatcher : IArgumentMatcher<string>
{
    private readonly Regex _regex;

    public RegexMatcher(string pattern) => _regex = new Regex(pattern);

    public RegexMatcher(Regex regex) => _regex = regex;

    public bool Matches(string? value) => value is not null && _regex.IsMatch(value);

    public bool Matches(object? value) => value is string s && Matches(s);

    public string Describe() => $"Arg.Matches(/{_regex}/)";
}
