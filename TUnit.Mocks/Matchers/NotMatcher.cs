using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// An argument matcher that negates an inner matcher — matches when the inner matcher does NOT match.
/// </summary>
internal sealed class NotMatcher<T> : IArgumentMatcher<T>
{
    private readonly IArgumentMatcher _inner;

    public NotMatcher(IArgumentMatcher inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool Matches(T? value) => !_inner.Matches(value);

    public bool Matches(object? value) => !_inner.Matches(value);

    public string Describe() => "Not(" + _inner.Describe() + ")";
}
