using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.AssertionBuilders;

public class AndConvertedTypeAssertionBuilder<TToType>(ISource source, ValueTask<AssertionData> mappedData)
    : ConvertedTypeAssertionBuilder<TToType>(source, mappedData), IAndAssertionBuilder;
        
public abstract class ConvertedTypeAssertionBuilder<TToType> : AssertionBuilder, IValueSource<TToType>
{
    // When using And/Or, we pop the last assertion off the stack to combine it.
    // However, when converting to a different type, it's a new assertion builder
    // Due to the new generic constraint, so we need to populate the stack with something
    internal ConvertedTypeAssertionBuilder(ISource source, 
        ValueTask<AssertionData> mappedData) 
        : base(mappedData, source.ActualExpression!, source.ExpressionBuilder, new Stack<BaseAssertCondition>([new NoOpWithMessageAssertionCondition<TToType>(source.Assertions.Peek().GetExpectationWithReason())]))
    {
        OtherTypeAssertionBuilder = source as IInvokableAssertionBuilder;
    }
}