namespace TUnit.Assertions.AssertConditions.ConditionEntries.Instance;

public class HasInstance<T>
{
    private readonly AsyncAssertCondition<T>? _asyncAssertCondition;
    private readonly IReadOnlyCollection<AssertCondition<T>> _assertConditions;
    
    public HasInstance(IReadOnlyCollection<AssertCondition<T>> assertConditions)
    {
        _assertConditions = assertConditions;
        And = new And<T>(assertConditions);
        Or = new Or<T>(assertConditions);
    }

    public And<T> And { get; }
    public Or<T> Or { get; }
}