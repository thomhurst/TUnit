namespace TUnit.Assertions.AssertConditions;

public class NotNullAssertCondition<TActual> : AssertCondition<TActual, TActual>
{
    public NotNullAssertCondition() : base(default)
    {
    }

    protected override string DefaultMessage => $"Member for {RawActualExpression ?? typeof(TActual).Name} was null";
    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        return actualValue is not null;
    }
}