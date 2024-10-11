using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableAssertionBuilder<TActual> : 
    AssertionBuilder<TActual>, IInvokableAssertionBuilder 
{
    internal InvokableAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual> assertionBuilder) : base(assertionDataDelegate, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
    }

    internal Task ProcessAssertionsAsync()
    {
        return ProcessAssertionDataAsync();
    }

    private async Task<AssertionData<TActual>?> ProcessAssertionDataAsync()
    {
        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();

        if (currentAssertionScope != null)
        {
            currentAssertionScope.Add(this);
            return null;
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

    internal async Task<TActual?> ProcessAssertionsWithValueAsync()
    {
        var assertionData = await ProcessAssertionDataAsync();
        if (assertionData != null)
        {
            return assertionData.Result;
        }

        return default;
    }

    internal async Task<TException?> ProcessAssertionsWithExceptionAsync<TException>()
        where TException : Exception
    {
        var assertionData = await ProcessAssertionDataAsync();
        return assertionData?.Exception as TException;
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

    public TaskAwaiter<TActual?> GetAwaiter() => ProcessAssertionsWithValueAsync().GetAwaiter();

    public TaskAwaiter GetAwaiterWithoutValue() => ProcessAssertionsAsync().GetAwaiter();

    public TaskAwaiter<TActual?> GetAwaiterWithValue() => ProcessAssertionsWithValueAsync().GetAwaiter();

    public TaskAwaiter<TException?> GetAwaiterWithException<TException>()
        where TException : Exception
        => ProcessAssertionsWithExceptionAsync<TException>().GetAwaiter();

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