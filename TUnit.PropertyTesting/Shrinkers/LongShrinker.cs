using TUnit.Core.Interfaces;

namespace TUnit.PropertyTesting.Shrinkers;

/// <summary>
/// Default shrinker for long values.
/// Shrinks toward zero through halving.
/// </summary>
public class LongShrinker : IShrinker<long>
{
    public IEnumerable<long> Shrink(long value)
    {
        if (value == 0)
        {
            yield break;
        }

        // Always try zero first
        yield return 0;

        // Then try progressively smaller values by halving
        var absValue = Math.Abs(value);
        var sign = Math.Sign(value);

        for (var i = absValue / 2; i > 0; i /= 2)
        {
            yield return sign * i;
        }
    }
}
