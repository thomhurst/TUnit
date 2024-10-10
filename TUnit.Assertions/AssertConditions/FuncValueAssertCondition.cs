namespace TUnit.Assertions.AssertConditions;

public class FuncValueAssertCondition<TActual, TExpected>(
    TExpected? expected,
    Func<TActual?, TExpected?, FuncValueAssertCondition<TActual, TExpected>, bool> condition,
    Func<TActual?, Exception?, string?, string> defaultMessageFactory
)
    : ExpectedValueAssertCondition<TActual, TExpected>(expected)
{
    protected override string GetExpectation()
        => $"to satisfy {this.GetType().Name}";

    protected internal override AssertionResult Passes(TActual? actualValue, TExpected? expectedValue)
    {
        // TODO VAB
        return AssertionResult.FailIf(
            () => !condition(actualValue, ExpectedValue, this),
            defaultMessageFactory(actualValue, Exception, Format(expectedValue)));
    }
}