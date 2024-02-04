using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsAnythingAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual, Exception, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public ThrowsAnythingAssertCondition(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder, default)
    {
    }
    
    protected override string DefaultMessage => "Nothing was thrown";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return exception != null;
    }
}