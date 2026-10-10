using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Matches only the exact same object instance, ignoring any <c>Equals</c> override.
/// </summary>
internal sealed class ReferenceEqualsMatcher<T> : IArgumentMatcher<T> where T : class
{
    private readonly T _expected;

    public ReferenceEqualsMatcher(T expected) => _expected = expected ?? throw new ArgumentNullException(nameof(expected));

    public bool Matches(T? value) => ReferenceEquals(_expected, value);

    public bool Matches(object? value) => ReferenceEquals(_expected, value);

    public string Describe() => "Arg.IsSameAs(" + _expected.GetType().Name + " instance)";
}
