using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions;

public abstract class BaseAssertCondition
{
    public AssertionResult FailWithMessage(string message)
    {
        OverriddenMessage = message;
        return AssertionResult.Fail(message);
    }
    
    public string? OverriddenMessage { get; internal set; }

    protected internal abstract string GetFailureMessage();

    protected string Format(object? obj)
    {
        if (obj is null)
        {
            return "null";
        }

        if (obj is string)
        {
            return $"""
                    "{obj}"
                    """;
        }

        if (obj is char)
        {
            return "'{obj}'";
        }

        if (obj is IEnumerable enumerable)
        {
            return $"[{string.Join(", ", enumerable.Cast<object>().Select(Format))}]";
        }

        return obj.ToString() ?? "null";
    }
}

public abstract class BaseAssertCondition<TActual> : BaseAssertCondition
{
    internal InvokableAssertionBuilder<TActual> ChainedToWithoutExpression(AssertionBuilder<TActual> assertionBuilder)
    {
        return assertionBuilder.WithAssertion(this);
    }
    
    internal InvokableAssertionBuilder<TActual> ChainedTo(AssertionBuilder<TActual> assertionBuilder, string[] argumentExpressions, [CallerMemberName] string caller = "")
    {
        return assertionBuilder.AppendExpression($"{caller}({string.Join(", ", argumentExpressions)})").WithAssertion(this);
    }
    
    internal AssertionResult Assert(AssertionData<TActual> assertionData)
    {
        return Assert(assertionData.Result, assertionData.Exception, assertionData.ActualExpression);
    }

    internal TActual? ActualValue { get; private set; }
    internal Exception? Exception { get; private set; }
    public string? ActualExpression { get; private set; }
    
    internal AssertionResult Assert(TActual? actualValue, Exception? exception, string? actualExpression)
    {
        ActualValue = actualValue;
        Exception = exception;
        ActualExpression = actualExpression;
        
        return Passes(actualValue, exception);
	}

	protected abstract AssertionResult Passes(TActual? actualValue, Exception? exception);

    protected string Because => _becauseReason?.ToString() ?? string.Empty;

    private BecauseReason? _becauseReason;
    
    internal virtual void SetBecauseReason(BecauseReason becauseReason)
    {
        if (_becauseReason != null)
        {
            // If multiple because reasons are provided, only apply them to the newer assertions.
            return;
        }
        
        _becauseReason = becauseReason;
    }
}
