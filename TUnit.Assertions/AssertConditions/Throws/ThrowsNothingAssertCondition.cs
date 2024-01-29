namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsNothingAssertCondition : DelegateAssertCondition
{
    public ThrowsNothingAssertCondition(IReadOnlyCollection<DelegateAssertCondition> nestedAssertConditions) : base(nestedAssertConditions)
    {
    }

    private Exception? _exception;

    public override string DefaultMessage => $"A {_exception?.GetType().Name} was thrown";

    protected override bool Passes(Exception? exception)
    {
        _exception = exception;
        return exception == null;
    }
}