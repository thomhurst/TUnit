using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableContainsAssertCondition<TActual, TInner, TAnd, TOr> : AssertCondition<TActual, TInner, TAnd, TOr>
    where TActual : IEnumerable<TInner>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public EnumerableContainsAssertCondition(AssertionBuilder<TActual> assertionBuilder, TInner expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => $"{ExpectedValue} was not found in the collection";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
            return false;
        }
        
        return actualValue.Contains(ExpectedValue);
    }
}