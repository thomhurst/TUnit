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
    
    /// <summary>
    /// Sets a timeout to wait for the assertion to complete.
    /// </summary>
    public virtual TimeSpan? WaitFor { get; protected set; }

    protected abstract string GetExpectation();

    internal string Expectation => GetExpectation();

    internal virtual string GetExpectationWithReason()
        => $"{GetExpectation()}{GetBecauseReason()}";

    internal abstract ValueTask<AssertionResult> GetAssertionResult(object? actualValue, Exception? exception, AssertionMetadata assertionMetadata, string? actualExpression);
    
    internal void SetSubject(string? subject)
        => Subject = subject;
}

public abstract class BaseAssertCondition<TActual> : BaseAssertCondition
{
    private ValueTask<AssertionResult>? _result;
    
    internal ValueTask<AssertionResult> GetAssertionResult(AssertionData assertionData)
    {
        return _result ??= GetAssertionResult(assertionData.Result, assertionData.Exception, new AssertionMetadata
        {
            StartTime = assertionData.Start,
            EndTime = assertionData.End
        }, assertionData.ActualExpression);
    }

    internal override ValueTask<AssertionResult> GetAssertionResult(object? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata, string? actualExpression)
    {
        if (actualValue is not null && actualValue is not TActual)
        {
            throw new AssertionException($"Expected {typeof(TActual).Name} but received {actualValue.GetType().Name}");
        }

        if (actualValue is null && typeof(TActual).IsValueType)
        {
            actualValue = default(TActual);
        }
        
        return _result ??= GetAssertionResult((TActual?) actualValue, exception, assertionMetadata, actualExpression);
    }

    internal TActual? ActualValue { get; private set; }
    internal Exception? Exception { get; private set; }
    public string? ActualExpression { get; private set; }
    
    public ValueTask<AssertionResult> GetAssertionResult(TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata, string? actualExpression = null)
    {
        ActualValue = actualValue;
        Exception = exception;
        ActualExpression = actualExpression;
        
        if (exception is not null)
        {
            AssertionScope.GetCurrentAssertionScope()?.RemoveException(exception);
        }

        return _result ??= GetResult(actualValue, exception, assertionMetadata);
    }

    protected abstract ValueTask<AssertionResult> GetResult(TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata);
}
