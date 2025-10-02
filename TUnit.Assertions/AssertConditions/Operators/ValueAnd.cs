using System.Text;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual>(AssertionCore assertionCore) : IValueSource<TActual>
{
    public static ValueAnd<TActual> Create(AssertionCore assertionCore)
    {
        return new ValueAnd<TActual>(assertionCore);
    }

    Stack<BaseAssertCondition> ISource.Assertions => ((ISource) assertionCore).Assertions;
    ValueTask<AssertionData> ISource.AssertionDataTask => ((ISource) assertionCore).AssertionDataTask;
    StringBuilder ISource.ExpressionBuilder => ((ISource) assertionCore).ExpressionBuilder;
    string? ISource.ActualExpression => ((ISource) assertionCore).ActualExpression;

    ISource ISource.AppendExpression(string expression)
    {
        ((ISource) assertionCore).AppendExpression(expression);
        return this;
    }

    ISource ISource.WithAssertion(BaseAssertCondition assertCondition)
    {
        ((ISource) assertionCore).WithAssertion(assertCondition);
        return this;
    }
}
