namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsNothingAssertCondition<TActual> : AssertCondition<TActual, TActual>
{
    public ThrowsNothingAssertCondition() : base(default)
    {
    }
    
    protected internal override string GetFailureMessage() => $"A {Exception?.GetType().Name} was thrown";

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return exception is null;
    }
}