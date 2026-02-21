using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Matches an empty collection.
/// </summary>
internal sealed class EmptyMatcher : IArgumentMatcher
{
    public bool Matches(object? value)
    {
        if (value is System.Collections.ICollection collection)
        {
            return collection.Count == 0;
        }
        if (value is System.Collections.IEnumerable enumerable)
        {
            foreach (var _ in enumerable)
            {
                return false;
            }
            return true;
        }
        return false;
    }

    public string Describe() => "Arg.IsEmpty()";
}
