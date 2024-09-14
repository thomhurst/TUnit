using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncValueDelegateAssertionBuilder<TActual> : AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> 
 {
    private readonly Func<Task<TActual>> _function;
    
    public Does<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> Does => new(this, ConnectorType.None, null);
    public Is<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> Is => new(this, ConnectorType.None, null);
    public Has<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> Has => new(this, ConnectorType.None, null);
    public Throws<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> Throws => new(this, ConnectorType.None, null);

    internal AsyncValueDelegateAssertionBuilder(Func<Task<TActual>> function, string expressionBuilder) : base(expressionBuilder)
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
            
    public AsyncValueDelegateAssertionBuilder<TActual> WithMessage(Func<TActual?, Exception?, string> message)
    {
        AssertionMessage = (AssertionMessageValueDelegate<TActual>) message;
        return this;
    }
    
    public AsyncValueDelegateAssertionBuilder<TActual> WithMessage(Func<string> message)
    {
        AssertionMessage = (AssertionMessageValueDelegate<TActual>) message;
        return this;
    }
}