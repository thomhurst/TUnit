namespace TUnit.Assertions.AssertConditions;

public class NotNullAssertCondition<TActual> : NullAssertCondition<TActual>
{
    public NotNullAssertCondition(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder)
    {
    }

    protected override string DefaultMessage => "Value is null";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return !base.Passes(actualValue, exception);
    }
}