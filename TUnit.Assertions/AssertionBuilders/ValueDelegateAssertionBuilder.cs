using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class ValueDelegateAssertionBuilder<TActual> : AssertionBuilder<TActual>
{
    private readonly Func<TActual> _function;
    
    public Does<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Does => new(this, ConnectorType.None, null);
    public Is<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Is => new(this, ConnectorType.None, null);
    public Has<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Has => new(this, ConnectorType.None, null);
    public Throws<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Throws => new(this, ConnectorType.None, null);

    internal ValueDelegateAssertionBuilder(Func<TActual> function, string expressionBuilder) : base(expressionBuilder)
    {
        _function = function;
    }

    protected internal override Task<AssertionData<TActual>> GetAssertionData()
    {
        var assertionData = _function.InvokeAndGetException();
        
        return Task.FromResult(assertionData);
    }
    
    public ValueDelegateAssertionBuilder<TActual> WithMessage(AssertionMessageValueDelegate<TActual> message)
    {
        AssertionMessage = message;
        return this;
    }
        
    public ValueDelegateAssertionBuilder<TActual> WithMessage(Func<TActual?, Exception?, string> message)
    {
        AssertionMessage = (AssertionMessageValueDelegate<TActual>) message;
        return this;
    }
    
    public ValueDelegateAssertionBuilder<TActual> WithMessage(Func<string> message)
    {
        AssertionMessage = (AssertionMessageValueDelegate<TActual>) message;
        return this;
    }
}