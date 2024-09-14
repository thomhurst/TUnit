using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class ValueDelegateAssertionBuilder<TActual, TAnd, TOr> 
    : AssertionBuilder<TActual, TAnd, TOr>,
        IIs<TActual, TAnd, TOr>,
        IHas<TActual, TAnd, TOr>,
        IDoes<TActual, TAnd, TOr>,
        IThrows<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr> 
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly Func<TActual> _function;

    Does<TActual, TAnd, TOr> IDoes<TActual, TAnd, TOr>.Does() => new(this, ConnectorType.None, null);
    DoesNot<TActual, TAnd, TOr> IDoes<TActual, TAnd, TOr>.DoesNot() => new(this, ConnectorType.None, null);
    Is<TActual, TAnd, TOr> IIs<TActual, TAnd, TOr>.Is() => new(this, ConnectorType.None, null);
    IsNot<TActual, TAnd, TOr> IIs<TActual, TAnd, TOr>.IsNot() => new(this, ConnectorType.None, null);
    Has<TActual, TAnd, TOr> IHas<TActual, TAnd, TOr>.Has() => new(this, ConnectorType.None, null);
    Throws<TActual, TAnd, TOr> IThrows<TActual, TAnd, TOr>.Throws() => new(this, ConnectorType.None, null);

    internal ValueDelegateAssertionBuilder(Func<TActual> function, string expressionBuilder) : base(expressionBuilder)
    {
        _function = function;
    }

    protected internal override Task<AssertionData<TActual>> GetAssertionData()
    {
        var assertionData = _function.InvokeAndGetException();
        
        return Task.FromResult(assertionData);
    }
    
    public ValueDelegateAssertionBuilder<TActual, TAnd, TOr> WithMessage(AssertionMessageValueDelegate<TActual> message)
    {
        AssertionMessage = message;
        return this;
    }
        
    public ValueDelegateAssertionBuilder<TActual, TAnd, TOr> WithMessage(Func<TActual?, Exception?, string> message)
    {
        AssertionMessage = (AssertionMessageValueDelegate<TActual>) message;
        return this;
    }
    
    public ValueDelegateAssertionBuilder<TActual, TAnd, TOr> WithMessage(Func<string> message)
    {
        AssertionMessage = (AssertionMessageValueDelegate<TActual>) message;
        return this;
    }

    private Is<TActual, TAnd, TOr> GetIs()
    {
        IIs<TActual, TAnd, TOr> @interface = this;
        return @interface.Is();
    }
}