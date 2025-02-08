using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertConditions;

public abstract class BaseAssertCondition
{
    private BecauseReason? _becauseReason;

    internal virtual void SetBecauseReason(BecauseReason becauseReason)
    {
        _becauseReason = becauseReason;
    }

    internal virtual string GetBecauseReason()
        => _becauseReason?.ToString() ?? string.Empty;

    public AssertionResult FailWithMessage(string message)
    {
        OverriddenMessage = message;
        
        return AssertionResult.Fail(message);
    }
    
    public string? OverriddenMessage { get; internal set; }
    
    public string? Subject { get; private set; }

    protected abstract string GetExpectation();

    internal virtual string GetExpectationWithReason()
        => $"{GetExpectation()}{GetBecauseReason()}";

    internal abstract Task<AssertionResult> GetAssertionResult(object? actualValue, Exception? exception, AssertionMetadata assertionMetadata, string? actualExpression);
    
    internal void SetSubject(string? subject)
        => Subject = subject;
}

public abstract class BaseAssertCondition<TActual> : BaseAssertCondition
{
    
    internal Task<AssertionResult> GetAssertionResult(AssertionData assertionData)
    {
        return GetAssertionResult(assertionData.Result, assertionData.Exception, new AssertionMetadata
        {
            StartTime = assertionData.Start,
            EndTime = assertionData.End
        }, assertionData.ActualExpression);
    }

    internal override Task<AssertionResult> GetAssertionResult(object? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata, string? actualExpression)
    {
        if (actualValue is not null && actualValue is not TActual)
        {
            throw new AssertionException($"Expected {typeof(TActual).Name} but received {actualValue.GetType().Name}");
        } 
        
        return GetAssertionResult((TActual?) actualValue, exception, assertionMetadata, actualExpression);
    }

    internal TActual? ActualValue { get; private set; }
    internal Exception? Exception { get; private set; }
    public string? ActualExpression { get; private set; }
    
    public Task<AssertionResult> GetAssertionResult(TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata, string? actualExpression = null)
    {
        ActualValue = actualValue;
        Exception = exception;
        ActualExpression = actualExpression;
        
        if (exception is not null)
        {
            AssertionScope.GetCurrentAssertionScope()?.RemoveException(exception);
        }

        return GetResult(actualValue, exception, assertionMetadata);
    }

    protected abstract Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata);
}
