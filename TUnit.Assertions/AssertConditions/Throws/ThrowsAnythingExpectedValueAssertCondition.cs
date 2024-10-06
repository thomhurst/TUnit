namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsAnythingExpectedValueAssertCondition<TActual>
    : ExpectedExceptionAssertCondition<TActual>
{
    protected internal override string GetFailureMessage() => "Nothing was thrown";

    protected override bool Passes(Exception? exception)
    {
        return exception != null;
    }
}