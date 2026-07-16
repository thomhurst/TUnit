using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// An argument matcher that matches values contained in a specified set.
/// </summary>
internal sealed class InSetMatcher<T> : IArgumentMatcher<T>
{
    private readonly HashSet<T> _values;

    public InSetMatcher(IEnumerable<T> values)
    {
        _values = new HashSet<T>(values);
    }

    public bool Matches(T? value)
    {
        if (value is null)
        {
            return _values.Contains(default!);
        }

        return _values.Contains(value);
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

    public string Describe()
    {
        var items = new List<string>();
        var count = 0;
        foreach (var v in _values)
        {
            if (count >= 5)
            {
                items.Add("...");
                break;
            }

            items.Add(v?.ToString() ?? "null");
            count++;
        }

        return "IsIn(" + string.Join(", ", items) + ")";
    }
}
