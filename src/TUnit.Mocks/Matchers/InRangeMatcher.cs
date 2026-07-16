using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// An argument matcher that matches values within an inclusive range [min, max].
/// </summary>
internal sealed class InRangeMatcher<T> : IArgumentMatcher<T> where T : IComparable<T>
{
    private readonly T _min;
    private readonly T _max;

    public InRangeMatcher(T min, T max)
    {
        _min = min;
        _max = max;
    }

    public bool Matches(T? value)
    {
        if (value is null)
        {
            return false;
        }

        return _min.CompareTo(value) <= 0 && _max.CompareTo(value) >= 0;
    }

    public bool Matches(object? value)
    {
        if (value is T typed)
        {
            return Matches(typed);
        }

        return false;
    }

    public string Describe() => "InRange(" + _min + ", " + _max + ")";
}
