using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateAnd<TActual>(AssertionBuilder<TActual> assertionBuilder) : IDelegateSource<TActual>
{
    public static DelegateAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new DelegateAnd<TActual>(assertionBuilder);
    }

    public string? ActualExpression => assertionBuilder.ActualExpression;

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