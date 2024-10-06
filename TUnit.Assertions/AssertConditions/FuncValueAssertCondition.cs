namespace TUnit.Assertions.AssertConditions;

public class FuncValueAssertCondition<TActual, TExpected>(
    TExpected? expected,
    Func<TActual?, TExpected?, FuncValueAssertCondition<TActual, TExpected>, bool> condition,
    Func<TActual?, Exception?, string?, string> defaultMessageFactory
)
    : ExpectedValueAssertCondition<TActual, TExpected>(expected)
{
    protected override string GetFailureMessage(TActual? actualValue, TExpected? expectedValue) =>
        defaultMessageFactory(actualValue, Exception, ActualExpression);

    protected override bool Passes(TActual? actualValue, TExpected? expectedValue)
    {
        return condition(actualValue, ExpectedValue, this);
    }
}