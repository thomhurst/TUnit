using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual>(AssertionBuilder<TActual> assertionBuilder) : IValueSource<TActual>
{
    public static ValueAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueAnd<TActual>(assertionBuilder);
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