using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueOr<TActual>(AssertionBuilder<TActual> assertionBuilder) : IValueSource<TActual>
{
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