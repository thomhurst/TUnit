using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertConditions;

public abstract class BaseAssertCondition
{
    protected internal virtual string? Message { get; }

    protected internal virtual string GetExtraMessage()
    {
        return string.Empty;
    }
    
    internal bool IsWrapped { get; set; }

    public abstract Task<bool> AssertAsync();
}

public abstract class BaseAssertCondition<TActual> : BaseAssertCondition
{
    public override async Task<bool> AssertAsync()
    {
        return Assert(await AssertionBuilder.AssertionDataDelegate());
    }

    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }
    
    protected string GetAssertionExpression()
    {
        var assertionExpression = AssertionBuilder.ExpressionBuilder?.ToString();
        
        if (assertionExpression?.TrimStart('"').TrimEnd('"')
            == ActualValue?.ToString())
        {
            return string.Empty;
        }
        
        return string.IsNullOrEmpty(assertionExpression)
            ? string.Empty
            : assertionExpression;
    }

    internal BaseAssertCondition(AssertionBuilder<TActual> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
        
        AssertionBuilder.Assertions.Add(this);
    }

    internal void AssertAndThrow(AssertionData<TActual> assertionData)
    {
        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();
        
        if (currentAssertionScope != null)
        {
            currentAssertionScope.Add(this);
            return;
        }
        
        if (!Assert(assertionData.Result, assertionData.Exception))
        {
            throw new AssertionException(
                $"""
                 {AssertionBuilder.AssertionMessage?.GetValue(assertionData.Result, assertionData.Exception)}
                 
                 {Message}
                 """.Trim()
            );
        }
    }

    internal TOutput ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(TAssertionBuilder assertionBuilder)
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return assertionBuilder.WithAssertion<TAssertionBuilder, TOutput>(this);
    }
    
    internal bool Assert(AssertionData<TActual> assertionData)
    {
        return Assert(assertionData.Result, assertionData.Exception);
    }

    protected TActual? ActualValue { get; private set; }
    protected Exception? Exception { get; private set; }


    protected internal override string Message =>
        $"""
         {GetAssertionExpression()}
         {MessageFactory?.Invoke(ActualValue, Exception) ?? DefaultMessage}{GetExtraMessage()}
         
         """;


    private Func<TActual?, Exception?, string>? MessageFactory { get; set; }
    
    public BaseAssertCondition<TActual> WithMessage(Func<TActual?, Exception?, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
    
    protected abstract string DefaultMessage { get; }
    
    internal bool Assert(TActual? actualValue, Exception? exception)
    {
        if (!IsWrapped)
        {
            throw new ArgumentException(
                $"{GetType().Name} isn't configured properly. It won't work with 'And' / 'Or' conditions. Call `AssertConditionCombiner.Combine(...)` to properly configure it.");
        }
        
        ActualValue = actualValue;
        Exception = exception;
        return Passes(actualValue, exception);
    }

    protected internal abstract bool Passes(TActual? actualValue, Exception? exception);
}