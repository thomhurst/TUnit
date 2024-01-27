namespace TUnit.Assertions;

public class ThrowsNothingAsyncAssertCondition : AsynchronousDelegateAssertCondition
{
    private Exception? _exception;
    
    public override string DefaultMessage => $"A {_exception?.GetType().Name} was thrown";
    
    protected override async Task<bool> Passes(Func<Task> actualValue)
    {
        try
        {
            await actualValue();
        }
        catch (Exception e)
        {
            _exception = e;
        }

        return _exception == null;
    }
}