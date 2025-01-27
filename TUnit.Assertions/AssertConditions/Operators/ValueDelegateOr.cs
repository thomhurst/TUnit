using System.Text;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateOr<TActual>(AssertionBuilder assertionBuilder) : IValueDelegateSource<TActual>
{
    public static ValueDelegateOr<TActual> Create(AssertionBuilder assertionBuilder)
    {
        return new ValueDelegateOr<TActual>(assertionBuilder);
    }
    
    public string? ActualExpression => assertionBuilder.ActualExpression;
    
    public Stack<BaseAssertCondition> Assertions => ((ISource)assertionBuilder).Assertions;
    public ValueTask<AssertionData> AssertionDataTask => ((ISource)assertionBuilder).AssertionDataTask;
    public StringBuilder ExpressionBuilder => ((ISource)assertionBuilder).ExpressionBuilder;
    
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