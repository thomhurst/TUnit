using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class DelegateAssertionBuilder
    : AssertionBuilder<object?>,
        IDelegateSource<object?>
{
    internal DelegateAssertionBuilder(Action action, string expressionBuilder) : base(action.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
    
    public new ISource<object?> AppendExpression(string expression)
    {
        base.AppendExpression(expression);
        return this;
    }

    public new ISource<object?> WithAssertion(BaseAssertCondition assertCondition)
    {
        base.WithAssertion(assertCondition);
        return this;
    }
}