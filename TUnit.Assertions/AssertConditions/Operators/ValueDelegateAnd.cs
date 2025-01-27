using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateAnd<TActual>(AssertionBuilder assertionBuilder) : IValueDelegateSource<TActual>
{
    public static ValueDelegateAnd<TActual> Create(AssertionBuilder assertionBuilder)
    {
        return new ValueDelegateAnd<TActual>(assertionBuilder);
    }
    
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