using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class AndConvertedDelegateAssertionBuilder<TToType>(ISource source, ValueTask<AssertionData> mappedData)
    : ConvertedDelegateAssertionBuilder<TToType>(source, mappedData), IAndAssertionBuilder;
        
public abstract class ConvertedDelegateAssertionBuilder<TToType> : AssertionBuilder, IValueSource<TToType>
{
    // When using And/Or, we pop the last assertion off the stack to combine it.
    // However, when converting to a different type, it's a new assertion builder
    // Due to the new generic constraint, so we need to populate the stack with something
    protected ConvertedDelegateAssertionBuilder(ISource source, 
        ValueTask<AssertionData> mappedData) 
        : base(mappedData, source.ActualExpression!, source.ExpressionBuilder, new Stack<BaseAssertCondition>([new NoOpAssertionCondition<TToType>(source.Assertions.Peek().Expectation)]))
    {
        OtherTypeAssertionBuilder = source as IInvokableAssertionBuilder;
    }
}

public class ConvertedValueAssertionBuilder<TFromType, TToType>(IValueSource<TFromType> source, ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition) 
    : InvokableValueAssertionBuilder<TToType>(new ConvertedValueSource<TFromType, TToType>(source, convertToAssertCondition));