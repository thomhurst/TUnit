using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Matches a collection with element-by-element equality.
/// </summary>
internal sealed class SequenceEqualsMatcher<T> : IArgumentMatcher
{
    private readonly T[] _expected;

    public SequenceEqualsMatcher(IEnumerable<T> expected) => _expected = expected.ToArray();

    public bool Matches(object? value)
    {
        if (value is IEnumerable<T> enumerable)
        {
            return enumerable.SequenceEqual(_expected);
        }
        return false;
    }

    public string Describe() => $"Arg.SequenceEquals([{string.Join(", ", _expected.Select(e => e?.ToString() ?? "null"))}])";
}
