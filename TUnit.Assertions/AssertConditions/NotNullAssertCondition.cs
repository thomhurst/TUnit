using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions;

public class NotNullAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual, TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public NotNullAssertCondition(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder, default)
    {
    }

    protected override string DefaultMessage => $"Member for {AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} was null";
    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue is not null;
    }
}