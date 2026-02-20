namespace TUnit.Mock.Matchers;

internal sealed class ExactMatcher<T> : Arguments.IArgumentMatcher<T>
{
    private readonly T? _expected;

    public ExactMatcher(T? expected) => _expected = expected;

    public bool Matches(T? value) => EqualityComparer<T>.Default.Equals(_expected!, value!);

    public bool Matches(object? value) => value is T typed ? Matches(typed) : _expected is null && value is null;

    public string Describe() => _expected?.ToString() ?? "null";
}
