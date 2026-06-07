namespace TUnit.Mocks.Matchers;

internal sealed class ExactMatcher<T> : Arguments.IArgumentMatcher<T>
{
    private readonly T? _expected;

    public ExactMatcher(T? expected)
    {
        // An Arg<X> can reach here boxed as the expected VALUE — e.g. Is(1) implicitly converted
        // into an Arg<object> slot, or Is<object>(someArg). Exact-matching the matcher struct
        // itself can never succeed, so fail fast with guidance.
        if (expected is not null && expected.GetType() is { IsConstructedGenericType: true } expectedType
            && expectedType.GetGenericTypeDefinition() == typeof(Arguments.Arg<>))
        {
            throw new ArgumentException(
                $"An Arg<{expectedType.GetGenericArguments()[0].Name}> matcher was passed where Arg<{typeof(T).Name}> was expected, " +
                $"so it would be matched as a literal value and never succeed. " +
                $"Use Arg.Is<{typeof(T).Name}>(value), Arg.Any<{typeof(T).Name}>(), or pass the raw value instead.");
        }

        _expected = expected;
    }

    public bool Matches(T? value) => EqualityComparer<T>.Default.Equals(_expected!, value!);

    public bool Matches(object? value) => value is T typed ? Matches(typed) : _expected is null && value is null;

    public string Describe() => _expected?.ToString() ?? "null";
}
