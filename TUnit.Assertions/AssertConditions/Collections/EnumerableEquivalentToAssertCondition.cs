namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableEquivalentToAssertCondition<T, TInner> : AssertCondition<T, IEnumerable<TInner>>
    where T : IEnumerable<TInner>
{
    public EnumerableEquivalentToAssertCondition(AssertionBuilder<T> assertionBuilder, IEnumerable<TInner> expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => "The two Enumerables were not equivalent";

    protected internal override bool Passes(T? actualValue, Exception? exception)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return true;
        }
        
        return actualValue?.SequenceEqual(ExpectedValue!) ?? false;
    }
}