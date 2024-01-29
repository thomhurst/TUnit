using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions.AssertConditions.ConditionEntries.Static;

public partial class Is
{
    public static AssertCondition<TExpected, TExpected> EqualTo<TExpected>(TExpected expected)
    {
        return new EqualsAssertCondition<TExpected, TExpected>([], null, expected);
    }
    
    internal static AssertCondition<TActual, TExpected> EqualTo<TActual, TExpected>(IReadOnlyCollection<AssertCondition<TActual, TExpected>> nestedConditions, NestedConditionsOperator? @operator, TExpected expected)
    {
        return new EqualsAssertCondition<TActual, TExpected>(nestedConditions, @operator, expected);
    }

    public static AssertCondition<T, T> SameReference<T>(T expected)
    {
        return new SameReferenceAssertCondition<T, T>([], null, expected);
    }

    internal static AssertCondition<T, T> SameReference<T>(IReadOnlyCollection<AssertCondition<T, T>> nestedConditions, NestedConditionsOperator? @operator, T expected)
    {
        return new SameReferenceAssertCondition<T, T>(nestedConditions, @operator, expected);
    }
}