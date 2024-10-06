using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<TActual, TExpected>(TExpected? expected, [CallerArgumentExpression("expected")] string expectedExpression = "") : BaseAssertCondition<TActual>
{
    protected TExpected? ExpectedValue { get; } = expected;
    public string ExpectedExpression { get; } = expectedExpression;
}