namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsAnythingAssertCondition<TActual>()
    : AssertCondition<TActual, Exception>(default)
{
    protected internal override string GetFailureMessage() => "Nothing was thrown";

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return exception != null;
    }
}