namespace TUnit.Assertions.AssertConditions;

public class NullAssertCondition<TActual> : AssertCondition<TActual, TActual>
{
    public NullAssertCondition(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder, default)
    {
    }

    protected override string DefaultMessage => $"{ActualValue} is not null";
    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue is null;
    }
}