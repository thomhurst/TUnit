using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions;

public class NotNullAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual, TActual, TAnd, TOr>
    where TAnd : And<TActual?, TAnd, TOr>, IAnd<TAnd, TActual?, TAnd, TOr>
    where TOr : Or<TActual?, TAnd, TOr>, IOr<TOr, TActual?, TAnd, TOr>
{
    public NotNullAssertCondition(AssertionBuilder<TActual?> assertionBuilder) : base(assertionBuilder!, default)
    {
    }

    protected override string DefaultMessage => $"Value for {typeof(TActual?).Name} was null";
    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue is not null;
    }
}