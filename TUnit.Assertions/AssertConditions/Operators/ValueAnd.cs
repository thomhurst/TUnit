using System.Text;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual>(AssertionBuilder assertionBuilder) : IValueSource<TActual>
{
    public static ValueAnd<TActual> Create(AssertionBuilder assertionBuilder)
    {
        return new ValueAnd<TActual>(assertionBuilder);
    }
    
    Stack<BaseAssertCondition> ISource.Assertions => ((ISource)assertionBuilder).Assertions;
    ValueTask<AssertionData> ISource.AssertionDataTask => ((ISource)assertionBuilder).AssertionDataTask;
    StringBuilder ISource.ExpressionBuilder => ((ISource)assertionBuilder).ExpressionBuilder;
    string? ISource.ActualExpression => ((ISource)assertionBuilder).ActualExpression;

    ISource ISource.AppendExpression(string expression)
    {
        ((ISource)assertionBuilder).AppendExpression(expression);
        return this;
    }

    ISource ISource.WithAssertion(BaseAssertCondition assertCondition)
    {
        ((ISource)assertionBuilder).WithAssertion(assertCondition);
        return this;
    }
}