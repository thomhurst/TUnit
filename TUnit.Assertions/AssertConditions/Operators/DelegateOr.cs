using System.Text;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateOr<TActual>(AssertionBuilder assertionBuilder) : IDelegateSource
{
    public static DelegateOr<TActual> Create(AssertionBuilder assertionBuilder)
    {
        return new DelegateOr<TActual>(assertionBuilder);
    }
    
    string? ISource.ActualExpression => ((ISource)assertionBuilder).ActualExpression;
    
    Stack<BaseAssertCondition> ISource.Assertions => ((ISource)assertionBuilder).Assertions;
    ValueTask<AssertionData> ISource.AssertionDataTask => ((ISource)assertionBuilder).AssertionDataTask;
    StringBuilder ISource.ExpressionBuilder => ((ISource)assertionBuilder).ExpressionBuilder;
    
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