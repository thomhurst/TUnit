using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public class DelegateAssertionBuilder<T> : AssertionBuilder<T>
{
    private readonly Func<T?> _function;
    
    public Throws<T> Throws => new(this, ConnectorType.None, null);

    internal DelegateAssertionBuilder(Func<T?> function)
    {
        _function = function;
    }

    protected internal override Task<AssertionData<T>> GetAssertionData()
    {
        var assertionData = _function.InvokeAndGetException();
        
        return Task.FromResult(assertionData)!;
    }
}

public class DelegateAssertionBuilder : AssertionBuilder<object?>
{
    private readonly Action _action;
    
    public Throws<object?> Throws => new(this, ConnectorType.None, null);

    internal DelegateAssertionBuilder(Action action)
    {
        _action = action;
    }

    protected internal override Task<AssertionData<object?>> GetAssertionData()
    {
        var exception = _action.InvokeAndGetException();
        
        return Task.FromResult(new AssertionData<object?>(null, exception));
    }
}