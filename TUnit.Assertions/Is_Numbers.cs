using System.Numerics;

namespace TUnit.Assertions;

public static partial class Is
{
    public static AssertCondition<T> GreaterThan<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanAssertCondition<T>(expected);
    }
}