namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableEquivalentToAssertCondition<T> : AssertCondition<IEnumerable<T>, IEnumerable<T>>
{
    public EnumerableEquivalentToAssertCondition(AssertionBuilder<IEnumerable<T>> assertionBuilder, IEnumerable<T> expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => "The two Enumerables were not equivalent";

    protected internal override bool Passes(IEnumerable<T>? actualValue, Exception? exception)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return true;
        }
        
        return actualValue?.SequenceEqual(ExpectedValue!) ?? false;
    }
}