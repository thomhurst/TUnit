using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public class DelegateAssertionBuilder<TActual> : AssertionBuilder<TActual>
{
    private readonly Func<TActual?> _function;
    
    public Does<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Does => new(this, ConnectorType.None, null);
    public Is<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Is => new(this, ConnectorType.None, null);
    public Has<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Has => new(this, ConnectorType.None, null);
    public Throws<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Throws => new(this, ConnectorType.None, null);

    internal DelegateAssertionBuilder(Func<TActual?> function)
    {
        _function = function;
    }

    protected internal override Task<AssertionData<TActual>> GetAssertionData()
    {
        var assertionData = _function.InvokeAndGetException();
        
        return Task.FromResult(assertionData)!;
    }
}

public class DelegateAssertionBuilder : AssertionBuilder<object?>
{
    private readonly Action _action;
    
    public Throws<object?, DelegateAnd<object?>, DelegateOr<object?>> Throws => new(this, ConnectorType.None, null);

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