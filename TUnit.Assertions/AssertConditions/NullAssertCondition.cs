namespace TUnit.Assertions.AssertConditions;

public class NullAssertCondition<TActual> : AssertCondition<TActual, TActual>
{
    public NullAssertCondition() : base(default)
    {
    }

    protected internal override string GetFailureMessage() => $"{ActualValue} is not null";
    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue is null;
    }
}