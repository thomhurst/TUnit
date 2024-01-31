using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public partial class Is
{
    public static AssertCondition<TExpected, TExpected> EqualTo<TExpected>(TExpected expected)
    {
        return new EqualsAssertCondition<TExpected, TExpected>(expected);
    }
    
    internal static AssertCondition<TActual, TExpected> EqualTo<TActual, TExpected>(TExpected expected)
    {
        return new EqualsAssertCondition<TActual, TExpected>(expected);
    }

    public static AssertCondition<T, T> SameReference<T>(T expected)
    {
        return new SameReferenceAssertCondition<T, T>(expected);
    }

    public static AssertCondition<object, object> Null => new NullAssertCondition();
    public static AssertCondition<object, TExpected> TypeOf<TExpected>() => new TypeOfAssertCondition<object, TExpected>();

    public static Not Not => new();
}