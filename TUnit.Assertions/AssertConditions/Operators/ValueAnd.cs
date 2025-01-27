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
    
    public Stack<BaseAssertCondition> Assertions => ((ISource)assertionBuilder).Assertions;
    public ValueTask<AssertionData> AssertionDataTask => ((ISource)assertionBuilder).AssertionDataTask;
    public StringBuilder ExpressionBuilder => ((ISource)assertionBuilder).ExpressionBuilder;
    
    public string? ActualExpression => assertionBuilder.ActualExpression;

    public ISource AppendExpression(string expression)
    {
        assertionBuilder.AppendExpression(expression);
        return this;
    }

    public ISource WithAssertion(BaseAssertCondition assertCondition)
    {
        assertionBuilder.WithAssertion(assertCondition);
        return this;
    }
}