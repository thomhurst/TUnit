using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncValueDelegateAssertionBuilder<TActual> : AssertionBuilder<TActual>
{
    private readonly Func<Task<TActual>> _function;
    
    public Does<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Does => new(this, ConnectorType.None, null);
    public Is<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Is => new(this, ConnectorType.None, null);
    public Has<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Has => new(this, ConnectorType.None, null);
    public Throws<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Throws => new(this, ConnectorType.None, null);

    internal AsyncValueDelegateAssertionBuilder(Func<Task<TActual>> function, string? expressionBuilder) : base(expressionBuilder)
    {
        _function = function;
    }

    protected internal override async Task<AssertionData<TActual>> GetAssertionData()
    {
        var assertionData = await _function.InvokeAndGetExceptionAsync();
        
        return assertionData;
    }
    
    public AsyncValueDelegateAssertionBuilder<TActual> WithMessage(AssertionMessageValueDelegate<TActual> message)
    {
        AssertionMessage = message;
        return this;
    }
}