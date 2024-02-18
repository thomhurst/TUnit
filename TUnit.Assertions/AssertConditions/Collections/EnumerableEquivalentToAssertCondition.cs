using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableEquivalentToAssertCondition<TActual, TInner, TAnd, TOr> : AssertCondition<TActual, IEnumerable<TInner>, TAnd, TOr>
    where TActual : IEnumerable<TInner>?
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public EnumerableEquivalentToAssertCondition(AssertionBuilder<TActual> assertionBuilder, IEnumerable<TInner> expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => $"""
                                                The two Enumerables were not equivalent
                                                   Actual: {(ActualValue != null ? string.Join(',', ActualValue) : null)}
                                                   Expected: {(ExpectedValue != null ? string.Join(',', ExpectedValue) : null)}
                                                """;

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return true;
        }
        
        return actualValue?.SequenceEqual(ExpectedValue!) ?? false;
    }
}