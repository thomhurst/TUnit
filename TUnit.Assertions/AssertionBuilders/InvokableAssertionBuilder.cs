using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableAssertionBuilder<TActual> : 
    AssertionBuilder<TActual>, IInvokableAssertionBuilder 
{
    internal InvokableAssertionBuilder(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.AssertionDataDelegate, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
    }

    internal async Task<AssertionData<TActual>> ProcessAssertionsAsync()
    {
        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();
        
        if (currentAssertionScope != null)
        {
            currentAssertionScope.Add(this);
            return null!;
        }

        var assertionData = await AssertionDataDelegate();
        
        foreach (var assertion in Assertions.Reverse())
        {
            var result = assertion.Assert(assertionData.Result, assertionData.Exception, assertionData.ActualExpression);
            if (!result.IsPassed)
            {
                assertion.SetSubject(assertionData.ActualExpression);
                throw new AssertionException(
                    $"""
                     Expected {assertion.Subject} {assertion.GetExpectationWithReason()}, but {result.Message}.
                     At {((IInvokableAssertionBuilder)this).GetExpression()}
                     """
                );
            }
        }
        
        return assertionData;
    }

    async IAsyncEnumerable<(BaseAssertCondition Assertion, AssertionResult Result)> IInvokableAssertionBuilder.GetFailures()
    {
        var assertionData = await AssertionDataDelegate();
        
        foreach (var assertion in Assertions.Reverse())
        {
            assertion.SetSubject(assertionData.ActualExpression);
            var result = assertion.Assert(assertionData.Result, assertionData.Exception, assertionData.ActualExpression);
            if (!result.IsPassed)
            {
                yield return (assertion, result);
            }
        }
    }

    public TaskAwaiter GetAwaiter() => ((Task) ProcessAssertionsAsync()).GetAwaiter();

    string? IInvokableAssertionBuilder.GetExpression()
    {
        var expression = ExpressionBuilder?.ToString();

        if (expression?.Length < 100)
        {
            return expression;
        }
        
        return $"{expression?[..100]}...";
    }
}