using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.AssertionBuilders;

public abstract class ConvertedTypeAssertionBuilder<TFromType, TToType> : AssertionBuilder, IValueSource<TToType>
{
    // When using And/Or, we pop the last assertion off the stack to combine it.
    // However, when converting to a different type, it's a new assertion builder
    // Due to the new generic constraint, so we need to populate the stack with something
    internal ConvertedTypeAssertionBuilder(InvokableAssertionBuilder<TFromType> otherTypeAssertionBuilder, ValueTask<AssertionData> actual,
        string actualExpression, StringBuilder expressionBuilder) 
        : base(actual, actualExpression, expressionBuilder, new Stack<BaseAssertCondition>([new NoOpWithMessageAssertionCondition<TToType>(otherTypeAssertionBuilder.Assertions.Peek().GetExpectationWithReason())]))
    {
        OtherTypeAssertionBuilder = otherTypeAssertionBuilder;
    }


    public new ISource AppendExpression(string expression)
    {
        OtherTypeAssertionBuilder?.AppendExpression(expression);
        return this;
    }

    public new ISource WithAssertion(BaseAssertCondition assertCondition)
    {
        OtherTypeAssertionBuilder?.WithAssertion(assertCondition);
        return this;
    }
}