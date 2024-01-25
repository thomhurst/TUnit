using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public static partial class Is
{
    public static IAssertCondition<T> EqualTo<T>(T expected)
    {
        return new EqualsAssertCondition<T>(expected);
    }
    
    public static IAssertCondition<T> SameReference<T>(T expected)
    {
        return new SameReferenceAssertCondition<T>(expected);
    }
}