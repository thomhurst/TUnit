namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsAnythingExpectedValueAssertCondition<TActual>
    : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetFailureMessage(Exception? exception) => "Nothing was thrown";

    protected override bool Passes(Exception? exception)
    {
        return exception != null;
    }
}