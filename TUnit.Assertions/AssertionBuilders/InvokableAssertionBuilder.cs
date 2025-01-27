using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableAssertionBuilder<TActual> : 
    AssertionBuilder, IInvokableAssertionBuilder 
{
    private readonly ISource _source;

    internal InvokableAssertionBuilder(ISource source) : base(source.AssertionDataTask, source.ActualExpression!,
        source.ExpressionBuilder, source.Assertions)
    {
        _source = source;
        
        if (source is InvokableAssertionBuilder<TActual> invokableAssertionBuilder)
        {
            AwaitedAssertionData = invokableAssertionBuilder.AwaitedAssertionData;
        }
    }

    internal async Task<T> ProcessAssertionsAsync<T>(Func<AssertionData, T> mapper)
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

    string IInvokableAssertionBuilder.GetExpression()
    {
        var expression = _source.ExpressionBuilder.ToString();

        if (expression.Length < 100)
        {
            return expression;
        }
        
        return $"{expression[..100]}...";
    }

    public Stack<BaseAssertCondition> Assertions => _source.Assertions;

    public new ISource AppendExpression(string expression)
    {
        base.AppendExpression(expression);
        return this;
    }

    public new ISource WithAssertion(BaseAssertCondition assertCondition)
    {
        base.WithAssertion(assertCondition);
        return this;
    }
}