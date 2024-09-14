using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;


public class ValueAssertionBuilder<TActual, TAnd, TOr> : AssertionBuilder<TActual, TAnd, TOr> 
    where TAnd : IAnd<TActual, TAnd, TOr> 
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly TActual _value;
    
    public Does<TActual, TAnd, TOr> Does => new(this, ConnectorType.None, null);
    public Is<TActual, TAnd, TOr> Is => new(this, ConnectorType.None, null);
    public Has<TActual, TAnd, TOr> Has => new(this, ConnectorType.None, null);

    internal ValueAssertionBuilder(TActual value, string expressionBuilder) : base(expressionBuilder)
    {
        _value = value;
    }

    protected internal override Task<AssertionData<TActual>> GetAssertionData()
    {
        return Task.FromResult(new AssertionData<TActual>(_value, null));
    }
    
    public ValueAssertionBuilder<TActual, TAnd, TOr> WithMessage(AssertionMessageValue<TActual> message)
    {
        AssertionMessage = message;
        return this;
    }
    
    public ValueAssertionBuilder<TActual, TAnd, TOr> WithMessage(Func<TActual?, string> message)
    {
        AssertionMessage = (AssertionMessageValue<TActual>) message;
        return this;
    }
    
    public ValueAssertionBuilder<TActual, TAnd, TOr> WithMessage(Func<string> message)
    {
        AssertionMessage = (AssertionMessageValue<TActual>) message;
        return this;
    }
}