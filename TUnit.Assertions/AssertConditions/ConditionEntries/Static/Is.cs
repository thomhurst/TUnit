using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions.AssertConditions.ConditionEntries.Static;

public partial class Is
{
    public static AssertCondition<T> EqualTo<T>(T expected)
    {
        return new EqualsAssertCondition<T, T>([], expected);
    }
    
    internal static AssertCondition<T> EqualTo<T>(IReadOnlyCollection<ExpectedValueAssertCondition<T, T>> previousConditions, T expected)
    {
        return new EqualsAssertCondition<T, T>(previousConditions, expected);
    }

    public static AssertCondition<T> SameReference<T>(T expected)
    {
        return new SameReferenceAssertCondition<T, T>([], expected);
    }

    internal static AssertCondition<T> SameReference<T>(IReadOnlyCollection<ExpectedValueAssertCondition<T, T>> previousConditions, T expected)
    {
        return new SameReferenceAssertCondition<T, T>(previousConditions, expected);
    }
}