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

    internal async Task ProcessAssertionsAsync()
    {
        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();
        
        if (currentAssertionScope != null)
        {
            currentAssertionScope.Add(this);
            return;
        }

        var assertionData = await AssertionDataDelegate();
        
        foreach (var assertion in Assertions.Reverse())
        {
            if (!assertion.Assert(assertionData))
            {
                throw new AssertionException(
                    $"""
                     {((IInvokableAssertionBuilder)this).GetExpression()}
                     {assertion.OverriddenMessage ?? assertion.GetFullFailureMessage()}
                     """
                );
            }
        }
    }

    async IAsyncEnumerable<BaseAssertCondition> IInvokableAssertionBuilder.GetFailures()
    {
        var assertionData = await AssertionDataDelegate();
        
        foreach (var assertion in Assertions.Reverse())
        {
            if (!assertion.Assert(assertionData))
            {
                yield return assertion;
            }
        }
    }

    public TaskAwaiter GetAwaiter() => ProcessAssertionsAsync().GetAwaiter();

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