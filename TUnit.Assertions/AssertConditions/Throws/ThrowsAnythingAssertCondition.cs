namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsAnythingAssertCondition<TActual> : AssertCondition<TActual, Exception>
{
    public ThrowsAnythingAssertCondition(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder, default)
    {
    }
    
    protected override string DefaultMessage => "Nothing was thrown";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return exception != null;
    }
}