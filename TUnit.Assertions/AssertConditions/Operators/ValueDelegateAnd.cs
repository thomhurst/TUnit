using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateAnd<TActual>(AssertionBuilder<TActual> assertionBuilder) : IValueDelegateSource<TActual>
{
    public static ValueDelegateAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueDelegateAnd<TActual>(assertionBuilder);
    }

    public ISource<TActual> AppendExpression(string expression)
    {
        assertionBuilder.AppendExpression(expression);
        return this;
    }

    public ISource<TActual> WithAssertion(BaseAssertCondition assertCondition)
    {
        assertionBuilder.WithAssertion(assertCondition);
        return this;
    }
}