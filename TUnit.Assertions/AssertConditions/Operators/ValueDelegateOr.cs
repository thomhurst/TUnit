using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateOr<TActual>(AssertionBuilder<TActual> assertionBuilder) : IValueDelegateSource<TActual>
{
    public static ValueDelegateOr<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueDelegateOr<TActual>(assertionBuilder);
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