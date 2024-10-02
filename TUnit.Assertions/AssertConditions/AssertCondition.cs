namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<TActual, TExpected>(TExpected? expected) : BaseAssertCondition<TActual>
{
    protected TExpected? ExpectedValue { get; } = expected;
}