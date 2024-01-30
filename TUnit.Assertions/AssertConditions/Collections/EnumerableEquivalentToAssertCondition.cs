namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableEquivalentToAssertCondition<T> : AssertCondition<IEnumerable<T>, IEnumerable<T>>
{
    public EnumerableEquivalentToAssertCondition(IEnumerable<T> expected) : base(expected)
    {
    }

    public override string DefaultMessage => "The two Enumerables were not equivalent";

    protected internal override bool Passes(IEnumerable<T> actualValue)
    {
        return actualValue.SequenceEqual(ExpectedValue!);
    }
}