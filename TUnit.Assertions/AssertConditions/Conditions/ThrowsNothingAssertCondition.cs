namespace TUnit.Assertions;

public class ThrowsNothingAssertCondition : SynchronousDelegateAssertCondition
{
    private Exception? _exception;
    
    public override string DefaultMessage => $"A {_exception?.GetType().Name} was thrown";
    
    protected override bool Passes(Action actualValue)
    {
        try
        {
            actualValue();
        }
        catch (Exception e)
        {
            _exception = e;
        }

        return _exception == null;
    }
}