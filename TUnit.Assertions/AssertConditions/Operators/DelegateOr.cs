using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateOr<TActual>(AssertionBuilder assertionBuilder) : IDelegateSource
{
    public static DelegateOr<TActual> Create(AssertionBuilder assertionBuilder)
    {
        return new DelegateOr<TActual>(assertionBuilder);
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