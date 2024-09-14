using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertConditions;

public abstract class BaseAssertCondition
{
    protected internal virtual string? Message { get; }
    internal abstract Task<bool> AssertAsync();

    protected internal virtual string GetExtraMessage()
    {
        return string.Empty;
    }
    
    internal bool IsWrapped { get; set; }
}

public abstract class BaseAssertCondition<TActual, TAnd, TOr> : BaseAssertCondition
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }
    
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

    internal BaseAssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
        
        And = TAnd.Create(this);
        Or = TOr.Create(this);
    }
    
    public TaskAwaiter GetAwaiter()
    {
        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();
        
        if (currentAssertionScope != null)
        {
            currentAssertionScope.Add(this);
            return Task.CompletedTask.GetAwaiter();
        }
        
        return AssertAndThrowAsync().GetAwaiter();
    }

    internal async Task AssertAndThrowAsync()
    {
        var assertionData = await AssertionBuilder.GetAssertionData();
        
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
    
    internal override async Task<bool> AssertAsync()
    {
        try
        {
            var assertionData = await AssertionBuilder.GetAssertionData();

            return Assert(assertionData.Result, assertionData.Exception);
        }
        catch (Exception e)
        {
            throw new AssertionException($"""
                                          {GetAssertionExpression()}
                                          {e.Message}
                                          """, e);
        }
    }

    protected TActual? ActualValue { get; private set; }
    protected Exception? Exception { get; private set; }


    protected internal override string Message =>
        $"""
         {GetAssertionExpression()}
         {MessageFactory?.Invoke(ActualValue, Exception) ?? DefaultMessage}{GetExtraMessage()}
         
         """;


    private Func<TActual?, Exception?, string>? MessageFactory { get; set; }
    
    public BaseAssertCondition<TActual, TAnd, TOr> WithMessage(Func<TActual?, Exception?, string> messageFactory)
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

    public TAnd And { get; }
    public TOr Or { get; }
}