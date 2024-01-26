using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public static partial class Is
{
    public static AssertCondition<T> EqualTo<T>(T expected)
    {
        return new EqualsAssertCondition<T, T>(expected);
    }
    
    public static AssertCondition<T> SameReference<T>(T expected)
    {
        return new SameReferenceAssertCondition<T, T>(expected);
    }
}