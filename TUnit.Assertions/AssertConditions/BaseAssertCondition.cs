using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertConditions;

public abstract class BaseAssertCondition
{
    public BaseAssertCondition()
    {
        AssertionsTracker.Current.Add(this);
    }
    
    protected internal virtual string? Message { get; }
    internal abstract Task<bool> AssertAsync();

    protected internal virtual string GetExtraMessage()
    {
        return string.Empty;
    }
}

public abstract class BaseAssertCondition<TActual, TAnd, TOr> : BaseAssertCondition
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
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
        
        And = TAnd.Create(this);
        Or = TOr.Create(this);
    }
    
    public TaskAwaiter GetAwaiter()
    {
        return AssertAndThrowAsync().GetAwaiter();
    }

    internal async Task AssertAndThrowAsync()
    {
        var assertionData = await AssertionBuilder.GetAssertionData();
        
        if (!Assert(assertionData.Result, assertionData.Exception))
        {
            throw new AssertionException(Message);
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
        ActualValue = actualValue;
        Exception = exception;
        return IsInverted ? !Passes(actualValue, exception) : Passes(actualValue, exception);
    }

    protected internal abstract bool Passes(TActual? actualValue, Exception? exception);

    public TAnd And { get; }
    public TOr Or { get; }

    internal BaseAssertCondition<TActual, TAnd, TOr> Invert(Func<TActual?, Exception?, string> messageFactory)
    {
        WithMessage(messageFactory);
        IsInverted = true;
        return this;
    }
    
    protected bool IsInverted { get; set; }
}