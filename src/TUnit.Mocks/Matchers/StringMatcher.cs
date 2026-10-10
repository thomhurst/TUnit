using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Matches non-null string arguments by prefix, suffix or substring.
/// </summary>
internal sealed class StringMatcher : IArgumentMatcher<string>
{
    internal enum Mode
    {
        StartsWith,
        EndsWith,
        Contains,
    }

    private readonly string _text;
    private readonly Mode _mode;
    private readonly StringComparison _comparison;

    public StringMatcher(Mode mode, string text, StringComparison comparison)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _mode = mode;
        _comparison = comparison;
    }

    public bool Matches(string? value)
    {
        if (value is null)
        {
            return false;
        }

        return _mode switch
        {
            Mode.StartsWith => value.StartsWith(_text, _comparison),
            Mode.EndsWith => value.EndsWith(_text, _comparison),
            _ => value.IndexOf(_text, _comparison) >= 0,
        };
    }

    public bool Matches(object? value) => value is string s && Matches(s);

    public string Describe() => _comparison == StringComparison.Ordinal
        ? $"Arg.{_mode}(\"{_text}\")"
        : $"Arg.{_mode}(\"{_text}\", {_comparison})";
}
