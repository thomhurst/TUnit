namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<TActual, TExpected> : BaseAssertCondition<TActual>
{
    internal TExpected? ExpectedValue { get; }
    
    internal AssertCondition(AssertionBuilder<TActual> assertionBuilder, TExpected? expected) : base(assertionBuilder)
    {
        ExpectedValue = expected;
    }
}