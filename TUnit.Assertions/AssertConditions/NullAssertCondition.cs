using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions;

public class NullAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual?, TActual?, TAnd, TOr>
    where TAnd : And<TActual?, TAnd, TOr>, IAnd<TAnd, TActual?, TAnd, TOr>
    where TOr : Or<TActual?, TAnd, TOr>, IOr<TOr, TActual?, TAnd, TOr>
{
    public NullAssertCondition(AssertionBuilder<TActual?> assertionBuilder) : base(assertionBuilder, default)
    {
    }

    protected override string DefaultMessage => $"{ActualValue} is not null";
    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue is null;
    }
}