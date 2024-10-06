namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsNothingExpectedValueAssertCondition<TActual> : ExpectedExceptionAssertCondition<TActual>
{
    protected internal override string GetFailureMessage() => $"A {Exception?.GetType().Name} was thrown";

    protected override bool Passes(Exception? exception)
    {
        return exception is null;
    }
}