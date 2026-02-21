using TUnit.Mock.Arguments;

namespace TUnit.Mock.Matchers;

/// <summary>
/// Matches a collection containing a specific item.
/// </summary>
internal sealed class ContainsMatcher<T> : IArgumentMatcher
{
    private readonly T _item;

    public ContainsMatcher(T item) => _item = item;

    public bool Matches(object? value)
    {
        if (value is IEnumerable<T> enumerable)
        {
            return enumerable.Contains(_item);
        }
        return false;
    }

    public string Describe() => $"Arg.Contains({_item})";
}
