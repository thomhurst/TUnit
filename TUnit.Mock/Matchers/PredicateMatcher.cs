using TUnit.Mock.Arguments;

namespace TUnit.Mock.Matchers;

/// <summary>
/// An argument matcher that delegates to a user-supplied predicate function.
/// </summary>
internal sealed class PredicateMatcher<T> : IArgumentMatcher<T>
{
    private readonly Func<T?, bool> _predicate;

    public PredicateMatcher(Func<T?, bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    public bool Matches(T? value) => _predicate(value);

    public bool Matches(object? value)
    {
        if (value is T typed)
        {
            return Matches(typed);
        }

        if (value is null)
        {
            return Matches(default);
        }

        return false;
    }

    public string Describe() => "Is<" + typeof(T).Name + ">(predicate)";
}
