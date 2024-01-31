namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableEquivalentToAssertCondition<T> : AssertCondition<IEnumerable<T>, IEnumerable<T>>
{
    public EnumerableEquivalentToAssertCondition(IEnumerable<T> expected) : base(expected)
    {
    }

    protected override string DefaultMessage => "The two Enumerables were not equivalent";

    protected internal override bool Passes(IEnumerable<T>? actualValue)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return true;
        }
        
        return actualValue?.SequenceEqual(ExpectedValue!) ?? false;
    }
}