namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsNothingExpectedValueAssertCondition<TActual> : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetFailureMessage(Exception? exception) => $"A {exception?.GetType().Name} was thrown";

    protected override bool Passes(Exception? exception)
    {
        return exception is null;
    }
}