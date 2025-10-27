using TUnit.Core.Interfaces;

namespace TUnit.PropertyTesting.Shrinkers;

/// <summary>
/// Default shrinker for string values.
/// Shrinks toward empty string by removing characters.
/// </summary>
public class StringShrinker : IShrinker<string>
{
    public IEnumerable<string> Shrink(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            yield break;
        }

        // Try empty string first
        yield return string.Empty;

        // Try progressively shorter strings by removing characters
        var length = value.Length;
        for (var newLength = length / 2; newLength > 0; newLength /= 2)
        {
            yield return value.Substring(0, newLength);
        }

        // Try removing one character at a time
        for (var i = 0; i < value.Length; i++)
        {
            yield return value.Remove(i, 1);
        }
    }
}
