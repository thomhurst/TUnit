using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions;

public class NullAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual, TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
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