using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableAssertionBuilder<TActual> : 
    AssertionBuilder<TActual>, IInvokableAssertionBuilder 
{
    internal InvokableAssertionBuilder(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.AssertionDataDelegate, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
        if (assertionBuilder is InvokableAssertionBuilder<TActual> invokableAssertionBuilder)
        {
            InvokedAssertionData = invokableAssertionBuilder.InvokedAssertionData;
        }
    }

    internal async Task<T> ProcessAssertionsAsync<T>(Func<AssertionData<TActual>, T> mapper)
    {
        var assertionData = await ProcessAssertionsAsync();
        return mapper(assertionData);
    }
    
    public TaskAwaiter GetAwaiter() => ((Task)ProcessAssertionsAsync()).GetAwaiter();
    
    public async Task<IEnumerable<AssertionResult>> GetAssertionResults()
    {
        await this;
        return Results;
    }

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