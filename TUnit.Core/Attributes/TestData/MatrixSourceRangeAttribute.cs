using System.Numerics;

namespace TUnit.Core;

#if NET7_0_OR_GREATER
[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixRangeAttribute<T>(T min, T max, T step)
    : MatrixAttribute<T>(CreateRange(min, max, step))
    where T : INumber<T>
{
    public MatrixRangeAttribute(T min, T max)
        : this(min, max, T.One)
    {
    }

    private static T?[] CreateRange(T min, T max, T step)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);

        return CreateRangeEnumerable(min, max, step).ToArray();
    }

    private static IEnumerable<T?> CreateRangeEnumerable(T min, T max, T step)
    {
        var current = min;

        while (current <= max)
        {
            yield return current;
            current += step;
        }
    }

}
#endif
