namespace TUnit.Assertions.AssertConditions.ConditionEntries.Instance;

public class HasInstance<T>
{
    private readonly IReadOnlyCollection<AssertCondition<T, T>> _assertConditions;
    
    public HasInstance(IReadOnlyCollection<AssertCondition<T, T>> assertConditions)
    {
        _assertConditions = assertConditions;
        And = new And<T, T>(assertConditions);
        Or = new Or<T, T>(assertConditions);
    }

    public And<T, T> And { get; }
    public Or<T, T> Or { get; }
}