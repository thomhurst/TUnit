using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions.AssertConditions.ConditionEntries.Static;

public partial class Is
{
    public static AssertCondition<TExpected, TExpected> EqualTo<TExpected>(TExpected expected)
    {
        return new EqualsAssertCondition<TExpected, TExpected>([], expected);
    }
    
    internal static AssertCondition<TActual, TExpected> EqualTo<TActual, TExpected>(IReadOnlyCollection<AssertCondition<TActual, TExpected>> previousConditions, TExpected expected)
    {
        return new EqualsAssertCondition<TActual, TExpected>(previousConditions, expected);
    }

    public static AssertCondition<T, T> SameReference<T>(T expected)
    {
        return new SameReferenceAssertCondition<T, T>([], expected);
    }

    internal static AssertCondition<T, T> SameReference<T>(IReadOnlyCollection<AssertCondition<T, T>> previousConditions, T expected)
    {
        return new SameReferenceAssertCondition<T, T>(previousConditions, expected);
    }
}