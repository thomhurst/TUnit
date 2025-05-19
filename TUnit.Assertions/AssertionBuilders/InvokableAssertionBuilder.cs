using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableAssertionBuilder<TActual> : 
    AssertionBuilder, IInvokableAssertionBuilder 
{
    protected readonly ISource Source;

    internal InvokableAssertionBuilder(ISource source) : base(source.AssertionDataTask, source.ActualExpression!,
        source.ExpressionBuilder, source.Assertions)
    {
        Source = source;
        
        if (source is InvokableAssertionBuilder<TActual> invokableAssertionBuilder)
        {
            AwaitedAssertionData = invokableAssertionBuilder.AwaitedAssertionData;
        }
    }

    internal async Task<T> ProcessAssertionsAsync<T>(Func<AssertionData, Task<T>> mapper)
    {
        var assertionData = await ProcessAssertionsAsync();
        return await mapper(assertionData);
    }
    
    public TaskAwaiter GetAwaiter() => ((Task)ProcessAssertionsAsync()).GetAwaiter();
    
    public async Task<IEnumerable<AssertionResult>> GetAssertionResults()
    {
        await this;
        return Results;
    }

    string IInvokableAssertionBuilder.GetExpression()
    {
        var expression = Source.ExpressionBuilder.ToString();

        if (expression.Length < 100)
        {
            return expression;
        }
        
        return $"{expression[..100]}...";
    }

    internal protected Stack<BaseAssertCondition> Assertions => Source.Assertions;
}