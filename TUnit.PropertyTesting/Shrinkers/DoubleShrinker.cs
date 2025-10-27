using TUnit.Core.Interfaces;

namespace TUnit.PropertyTesting.Shrinkers;

/// <summary>
/// Default shrinker for double values.
/// Shrinks toward zero through halving.
/// </summary>
public class DoubleShrinker : IShrinker<double>
{
    public IEnumerable<double> Shrink(double value)
    {
        if (value == 0.0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            yield break;
        }

        // Always try zero first
        yield return 0.0;

        // Then try progressively smaller values by halving
        var absValue = Math.Abs(value);
        var sign = Math.Sign(value);

        var current = absValue / 2.0;
        while (current > 0.0 && current != absValue)
        {
            yield return sign * current;
            current /= 2.0;

            // Prevent infinite loop for very small values
            if (Math.Abs(current) < double.Epsilon)
            {
                break;
            }
        }
    }
}
