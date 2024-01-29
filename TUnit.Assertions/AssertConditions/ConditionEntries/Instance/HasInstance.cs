namespace TUnit.Assertions.AssertConditions.ConditionEntries.Instance;

public class HasInstance<T>
{
    private readonly IReadOnlyCollection<AssertCondition<T>> _assertConditions;

    public HasInstance(AssertCondition<T> assertConditions) : this([assertConditions])
    {
    }
    
    public HasInstance(IReadOnlyCollection<AssertCondition<T>> assertConditions)
    {
        _assertConditions = assertConditions;
        And = new And<T>(assertConditions);
        Or = new Or<T>(assertConditions);
    }

    public And<T> And { get; }
    public Or<T> Or { get; }
}