namespace TUnit.Assertions.AssertConditions.ConditionEntries.Instance;

public class HasInstance<T>
{
    public HasInstance(AssertCondition<T, T> otherAssertConditions)
    {
        And = new And<T, T>(otherAssertConditions);
        Or = new Or<T, T>(otherAssertConditions);
    }

    public And<T, T> And { get; }
    public Or<T, T> Or { get; }
}