namespace TUnit.Assertions.AssertConditions;

public class NullAssertCondition<TActual> : AssertCondition<TActual, TActual>
{
    public NullAssertCondition() : base(default)
    {
    }

    protected override string DefaultMessage => $"{ActualValue} is not null";
    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        return actualValue is null;
    }
}