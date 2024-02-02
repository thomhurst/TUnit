namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableContainsAssertCondition<T, TInner> : AssertCondition<T, TInner>
    where T : IEnumerable<TInner>
{
    public EnumerableContainsAssertCondition(AssertionBuilder<T> assertionBuilder, TInner expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => $"{ExpectedValue} was not found in the collection";

    protected internal override bool Passes(T? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            return false;
        }
        
        return actualValue.Contains(ExpectedValue);
    }
}