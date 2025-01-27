using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class ValueDelegateAssertionBuilder<TActual> 
    : AssertionBuilder<TActual>, 
        IDelegateSource<TActual>, 
        IValueSource<TActual>
{
    internal ValueDelegateAssertionBuilder(Func<TActual> function, string expressionBuilder) : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }

    public new ISource<TActual> AppendExpression(string expression)
    {
        base.AppendExpression(expression);
        return this;
    }
    
    public new ISource<TActual> WithAssertion(BaseAssertCondition assertCondition)
    {
        base.WithAssertion(assertCondition);
        return this;
    }
}