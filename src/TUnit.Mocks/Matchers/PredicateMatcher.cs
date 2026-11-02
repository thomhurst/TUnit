using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// An argument matcher that delegates to a user-supplied predicate function.
/// </summary>
internal sealed class PredicateMatcher<T> : IArgumentMatcher<T>
{
    private readonly Func<T, bool> _predicate;

    public PredicateMatcher(Func<T, bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    public bool Matches(T? value)
    {
        if (value is not null)
        {
            return _predicate(value);
        }

        // Is<T> and Is<T?> share one runtime type, so a null argument still reaches the predicate:
        // a null-aware predicate can match it in either form. Any NRE on this null-only path becomes
        // a non-match, including one from unrelated predicate logic; see Arg.Is<T>'s remarks.
        try
        {
            return _predicate(value!);
        }
        catch (NullReferenceException)
        {
            return false;
        }
    }

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
