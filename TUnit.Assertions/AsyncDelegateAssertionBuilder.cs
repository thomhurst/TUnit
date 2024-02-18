using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions;

public class AsyncDelegateAssertionBuilder<TActual> : AssertionBuilder<TActual>
{
    private readonly Func<Task<TActual>> _function;
    
    public Does<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Does => new(this, ConnectorType.None, null);
    public Is<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Is => new(this, ConnectorType.None, null);
    public Has<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Has => new(this, ConnectorType.None, null);
    public Throws<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Throws => new(this, ConnectorType.None, null);

    internal AsyncDelegateAssertionBuilder(Func<Task<TActual>> function, AssertionMessageValueDelegate<TActual>? message, string? expressionBuilder) : base(expressionBuilder, message)
    {
        _function = function;
    }

    protected internal override async Task<AssertionData<TActual>> GetAssertionData()
    {
        var assertionData = await _function.InvokeAndGetExceptionAsync();
        
        return assertionData;
    }
}

public class AsyncDelegateAssertionBuilder : AssertionBuilder<object?>
{
    private readonly Func<Task> _function;
    
    public Throws<object?, DelegateAnd<object?>, DelegateOr<object?>> Throws => new(this, ConnectorType.None, null);

    internal AsyncDelegateAssertionBuilder(Func<Task> function, AssertionMessageDelegate? message,
        string? expressionBuilder) : base(expressionBuilder, message)
    {
        _function = function;
    }

    protected internal override async Task<AssertionData<object?>> GetAssertionData()
    {
        var exception = await _function.InvokeAndGetExceptionAsync();
        
        return new AssertionData<object?>(null, exception);
    }
}