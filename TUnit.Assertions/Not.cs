using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public class Not<TActual>
{
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }

    public Not(AssertionBuilder<TActual> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public AssertCondition<TActual, TActual> Null => new NotNullAssertCondition<TActual>(AssertionBuilder);
    public AssertCondition<TActual, TExpected> TypeOf<TExpected>() => new NotTypeOfAssertCondition<TActual, TExpected>(AssertionBuilder);
}