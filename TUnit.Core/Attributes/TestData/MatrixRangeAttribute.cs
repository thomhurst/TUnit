using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace TUnit.Core;

#if NET7_0_OR_GREATER
[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixRangeAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T min, T max)
    : MatrixAttribute<T>(CreateRange(min, max))
    where T : INumber<T>
{
    private static T?[]? CreateRange(T min, T max)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);

        return CreateRangeEnumerable(min, max).ToArray();
    }

    private static IEnumerable<T?> CreateRangeEnumerable(T min, T max)
    {
        var current = min;
        
        while (current <= max)
        {
            yield return current;
            current += T.One;
        }
    }

}
#endif