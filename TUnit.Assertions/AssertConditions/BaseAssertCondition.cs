using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;

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
    
    internal void SetSubject(string? subject)
        => Subject = subject;
}

public abstract class BaseAssertCondition<TActual> : BaseAssertCondition
{
    internal InvokableAssertionBuilder<TActual> ChainedToWithoutExpression(AssertionBuilder<TActual> assertionBuilder)
    {
        return assertionBuilder.WithAssertion(this);
    }
    
    internal InvokableAssertionBuilder<TActual> ChainedTo(AssertionBuilder<TActual> assertionBuilder, string?[] argumentExpressions, [CallerMemberName] string caller = "")
    {
        if (string.IsNullOrEmpty(caller))
        {
            return assertionBuilder.WithAssertion(this);
        }

        assertionBuilder.AppendExpression(caller)
            .AppendRaw('(');

        argumentExpressions = argumentExpressions.OfType<string>().ToArray();
        
        for (var index = 0; index < argumentExpressions.Length; index++)
        {
            var argumentExpression = argumentExpressions[index];

            if (string.IsNullOrEmpty(argumentExpression))
            {
                continue;
            }
            
            assertionBuilder.AppendRaw(argumentExpression!);

            if (index < argumentExpressions.Length - 1)
            {
                assertionBuilder.AppendRaw(',');
                assertionBuilder.AppendRaw(' ');
            }
        }

        return assertionBuilder.AppendRaw(')')
            .WithAssertion(this);
    }
    
    internal Task<AssertionResult> Assert(AssertionData<TActual> assertionData)
    {
        return Assert(assertionData.Result, assertionData.Exception, assertionData.ActualExpression);
    }

    internal TActual? ActualValue { get; private set; }
    internal Exception? Exception { get; private set; }
    public string? ActualExpression { get; private set; }
    
    internal Task<AssertionResult> Assert(TActual? actualValue, Exception? exception, string? actualExpression)
    {
        ActualValue = actualValue;
        Exception = exception;
        ActualExpression = actualExpression;
        
        if (exception is not null)
        {
            AssertionScope.GetCurrentAssertionScope()?.RemoveException(exception);
        }

        return GetResult(actualValue, exception);
    }

    protected abstract Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception);
}
