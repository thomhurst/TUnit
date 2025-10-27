using TUnit.Core.Interfaces;

namespace TUnit.PropertyTesting.Shrinkers;

/// <summary>
/// Default shrinker for boolean values.
/// Shrinks true to false (false cannot be shrunk further).
/// </summary>
public class BoolShrinker : IShrinker<bool>
{
    public IEnumerable<bool> Shrink(bool value)
    {
        // Only true can be shrunk to false
        if (value)
        {
            yield return false;
        }

        // false cannot be shrunk further
    }
}
