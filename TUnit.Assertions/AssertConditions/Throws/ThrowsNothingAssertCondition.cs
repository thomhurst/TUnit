using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsNothingAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual, TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    public ThrowsNothingAssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) : base(assertionBuilder, default)
    {
    }
    
    protected override string DefaultMessage => $"A {Exception?.GetType().Name} was thrown";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return exception is null;
    }
}