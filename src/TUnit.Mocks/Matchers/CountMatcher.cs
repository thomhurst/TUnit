using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Matches a collection with a specific number of elements.
/// </summary>
internal sealed class CountMatcher : IArgumentMatcher
{
    private readonly int _expectedCount;

    public CountMatcher(int expectedCount) => _expectedCount = expectedCount;

    public bool Matches(object? value)
    {
        if (value is System.Collections.ICollection collection)
        {
            return collection.Count == _expectedCount;
        }
        if (value is System.Collections.IEnumerable enumerable)
        {
            int count = 0;
            foreach (var _ in enumerable)
            {
                count++;
            }
            return count == _expectedCount;
        }
        return false;
    }

    public string Describe() => $"Arg.HasCount({_expectedCount})";
}
