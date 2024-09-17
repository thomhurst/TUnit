namespace TUnit.Assertions.AssertConditions;

public class NotNullAssertCondition<TActual> : AssertCondition<TActual, TActual>
{
    public NotNullAssertCondition() : base(default)
    {
    }

    protected internal override string GetFailureMessage() => $"Member for {ActualExpression ?? typeof(TActual).Name} was null";
    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue is not null;
    }
}