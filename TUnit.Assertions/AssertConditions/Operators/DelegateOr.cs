using System.Text;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateOr<TActual>(AssertionCore assertionCore) : IDelegateSource
{
    public static DelegateOr<TActual> Create(AssertionCore assertionCore)
    {
        return new DelegateOr<TActual>(assertionCore);
    }

    string? ISource.ActualExpression => ((ISource) assertionCore).ActualExpression;

    Stack<BaseAssertCondition> ISource.Assertions => ((ISource) assertionCore).Assertions;
    ValueTask<AssertionData> ISource.AssertionDataTask => ((ISource) assertionCore).AssertionDataTask;
    StringBuilder ISource.ExpressionBuilder => ((ISource) assertionCore).ExpressionBuilder;

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
