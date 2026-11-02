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
        // a nullable-aware predicate can match it. A predicate written for non-nullable T dereferences
        // it instead, and that null argument does not match rather than failing the mocked call.
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
