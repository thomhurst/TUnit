using System.Runtime.CompilerServices;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableAssertionBuilder<TActual> : 
    AssertionBuilder<TActual>, IInvokableAssertionBuilder 
{
    private AssertionData<TActual>? _invokedAssertionData;

    internal InvokableAssertionBuilder(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.AssertionDataDelegate, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
        if (assertionBuilder is InvokableAssertionBuilder<TActual> invokableAssertionBuilder)
        {
            _invokedAssertionData = invokableAssertionBuilder._invokedAssertionData;
        }
    }

    internal async Task<AssertionData<TActual>> ProcessAssertionsAsync()
    {
        _invokedAssertionData ??= await AssertionDataDelegate();

        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();
        
        foreach (var assertion in Assertions.Reverse())
        {
            var result = await assertion.Assert(_invokedAssertionData.Result, _invokedAssertionData.Exception, _invokedAssertionData.ActualExpression);
            if (!result.IsPassed)
            {
                assertion.SetSubject(_invokedAssertionData.ActualExpression);

                var exception = new AssertionException(
                    $"""
                     Expected {assertion.Subject} {assertion.GetExpectationWithReason()}, but {result.Message}.
                     At {((IInvokableAssertionBuilder)this).GetExpression()}
                     """
                );
                
                if (currentAssertionScope != null)
                {
                    currentAssertionScope.AddException(exception);
                    continue;
                }
                
                throw exception;
            }
        }
        
        return _invokedAssertionData;
    }
    
    public TaskAwaiter GetAwaiter() => ((Task)ProcessAssertionsAsync()).GetAwaiter();

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