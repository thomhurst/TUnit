using System.Numerics;
using TUnit.Assertions.AssertConditions.Numbers;

namespace TUnit.Assertions;

public static partial class Is
{
    public static AssertCondition<T> GreaterThan<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanAssertCondition<T, T>(expected);
    }
}