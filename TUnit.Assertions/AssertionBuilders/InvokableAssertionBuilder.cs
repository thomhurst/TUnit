using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableAssertionBuilder<TActual> : 
    AssertionBuilder, IInvokableAssertionBuilder 
{
    private readonly AssertionBuilder _assertionBuilder;

    internal InvokableAssertionBuilder(AssertionBuilder assertionBuilder) : base(((ISource)assertionBuilder).AssertionDataTask, assertionBuilder.ActualExpression!, (
        (ISource)assertionBuilder).ExpressionBuilder, ((ISource)assertionBuilder).Assertions)
    {
        _assertionBuilder = assertionBuilder;
        if (assertionBuilder is InvokableAssertionBuilder<TActual> invokableAssertionBuilder)
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
        var expression = ((ISource)_assertionBuilder).ExpressionBuilder.ToString();

        if (expression?.Length < 100)
        {
            return expression;
        }
        
        return $"{expression?[..100]}...";
    }

    public Stack<BaseAssertCondition> Assertions => ((ISource)_assertionBuilder).Assertions;

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