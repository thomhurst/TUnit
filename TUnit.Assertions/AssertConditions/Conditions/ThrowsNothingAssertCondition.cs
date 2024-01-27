namespace TUnit.Assertions.AssertConditions.Conditions;

public class ThrowsNothingAssertCondition : DelegateAssertCondition
{
    private Exception? _exception;
    
    public override string DefaultMessage => $"A {_exception?.GetType().Name} was thrown";
    
    protected override bool Passes(Exception? exception)
    {
        _exception = exception;
        return exception == null;
    }
}