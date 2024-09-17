namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<TActual, TExpected>(TExpected? expected) : BaseAssertCondition<TActual>
{
    internal TExpected? ExpectedValue { get; } = expected;
}