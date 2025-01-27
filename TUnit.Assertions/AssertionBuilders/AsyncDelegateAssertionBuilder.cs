using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncDelegateAssertionBuilder 
    : AssertionBuilder<object?>,
        IDelegateSource<object?>
{
    internal AsyncDelegateAssertionBuilder(Func<Task> function, string expressionBuilder) : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
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