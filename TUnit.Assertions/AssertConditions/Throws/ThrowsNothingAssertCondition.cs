using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsNothingAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual?, TActual?, TAnd, TOr>
    where TAnd : And<TActual?, TAnd, TOr>, IAnd<TAnd, TActual?, TAnd, TOr>
    where TOr : Or<TActual?, TAnd, TOr>, IOr<TOr, TActual?, TAnd, TOr>
{
    public ThrowsNothingAssertCondition(AssertionBuilder<TActual?> assertionBuilder) : base(assertionBuilder, default)
    {
    }
    
    protected override string DefaultMessage => $"A {Exception?.GetType().Name} was thrown";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return exception is null;
    }
}