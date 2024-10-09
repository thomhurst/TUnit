﻿using System.Runtime.CompilerServices;
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
            var result = assertion.Passes(assertionData.Result, assertionData.Exception);
            if (!result.IsPassed)
            {
                throw new AssertionException(
                    $"""
                     Expected {assertionData.ActualExpression} {assertion.GetFailureMessage()}{assertion.Because} but {result.Message}
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
			var result = assertion.Passes(assertionData.Result, assertionData.Exception);
			if (!result.IsPassed)
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