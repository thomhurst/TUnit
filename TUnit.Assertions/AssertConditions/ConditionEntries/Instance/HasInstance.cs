namespace TUnit.Assertions.AssertConditions.ConditionEntries.Instance;

public class HasInstance<T>
{
    private readonly AssertCondition<T, T> _otherAssertConditions;

    public HasInstance(AssertCondition<T, T> otherAssertConditions)
    {
        _otherAssertConditions = otherAssertConditions;
    }

    
}