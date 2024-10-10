namespace TUnit.Assertions.AssertConditions;

public class FuncValueAssertCondition<TActual, TExpected>(
    TExpected? expected,
    Func<TActual?, TExpected?, FuncValueAssertCondition<TActual, TExpected>, bool> condition,
    Func<TActual?, Exception?, string?, string> defaultMessageFactory
)
    : ExpectedValueAssertCondition<TActual, TExpected>(expected)
{
    protected override string GetExpectation()
        => $"to satisfy {GetType().Name}";

    protected override AssertionResult GetResult(TActual? actualValue, TExpected? expectedValue)
    {
        return AssertionResult.FailIf(
            () => !condition(actualValue, ExpectedValue, this),
            defaultMessageFactory(actualValue, Exception, Format(expectedValue)));
    }
}