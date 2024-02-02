using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public partial class Is<TActual>
{
    internal readonly AssertionBuilder<TActual> AssertionBuilder;

    public Is(AssertionBuilder<TActual> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public AssertCondition<TActual, TActual> EqualTo(TActual expected)
    {
        return new EqualsAssertCondition<TActual, TActual>(AssertionBuilder, expected);
    }
    
    internal AssertCondition<TActual, TExpected> EqualTo<TExpected>(TExpected expected)
    {
        return new EqualsAssertCondition<TActual, TExpected>(AssertionBuilder, expected);
    }

    public AssertCondition<TActual, TActual> SameReference(TActual expected)
    {
        return new SameReferenceAssertCondition<TActual, TActual>(AssertionBuilder, expected);
    }

    public AssertCondition<TActual, TActual> Null => new NullAssertCondition<TActual>(AssertionBuilder);
    public AssertCondition<TActual, TExpected> TypeOf<TExpected>() => new TypeOfAssertCondition<TActual, TExpected>(AssertionBuilder);

    public Not<TActual> Not => new(AssertionBuilder);
}