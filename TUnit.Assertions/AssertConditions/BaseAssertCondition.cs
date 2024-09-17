using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions;

public abstract class BaseAssertCondition
{
    protected internal virtual string? Message { get; }

    protected internal virtual string GetExtraMessage()
    {
        return string.Empty;
    }
}

public abstract class BaseAssertCondition<TActual> : BaseAssertCondition
{
    internal InvokableAssertionBuilder<TActual, TAnd, TOr> ChainedTo<TAnd, TOr>(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return assertionBuilder.WithAssertion(this);
    }
    
    internal bool Assert(AssertionData<TActual> assertionData)
    {
        return Assert(assertionData.Result, assertionData.Exception, assertionData.ActualExpression);
    }

    protected TActual? ActualValue { get; private set; }
    protected Exception? Exception { get; private set; }
    protected string? RawActualExpression { get; private set; }


    protected internal override string Message =>
        $"{MessageFactory?.Invoke(ActualValue, Exception, RawActualExpression) ?? DefaultMessage}{GetExtraMessage()}";


    private Func<TActual?, Exception?, string?, string>? MessageFactory { get; set; }
    
    public BaseAssertCondition<TActual> WithMessage(Func<TActual?, Exception?, string?, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
    
    protected abstract string DefaultMessage { get; }
    
    internal bool Assert(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        ActualValue = actualValue;
        Exception = exception;
        RawActualExpression = rawValueExpression;
        
        return Passes(actualValue, exception, rawValueExpression);
    }

    protected internal abstract bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression);
}