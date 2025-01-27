using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncValueDelegateAssertionBuilder<TActual> 
    : AssertionBuilder, IValueDelegateSource<TActual>
 {
    internal AsyncValueDelegateAssertionBuilder(Func<Task<TActual>> function, string? expressionBuilder) : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }

    public new ISource AppendExpression(string expression)
    {
        base.AppendExpression(expression);
        return this;
    }

    public new ISource WithAssertion(BaseAssertCondition assertCondition)
    {
        base.WithAssertion(assertCondition);
        return this;
    }
 }