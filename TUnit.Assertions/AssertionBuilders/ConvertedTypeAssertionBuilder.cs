using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.AssertionBuilders;

public abstract class ConvertedTypeAssertionBuilder<TFromType, TToType> : AssertionBuilder<TToType>, IValueSource<TToType>
{
    // When using And/Or, we pop the last assertion off the stack to combine it.
    // However, when converting to a different type, it's a new assertion builder
    // Due to the new generic constraint, so we need to populate the stack with something
    internal ConvertedTypeAssertionBuilder(InvokableAssertionBuilder<TFromType> otherTypeAssertionBuilder, Func<Task<AssertionData<TToType>>> actual,
        string actualExpression, StringBuilder expressionBuilder) 
        : base(actual, actualExpression, expressionBuilder, new Stack<BaseAssertCondition<TToType>>([new NoOpWithMessageAssertionCondition<TToType>(otherTypeAssertionBuilder.Assertions.Peek().GetExpectationWithReason())]))
    {
        OtherTypeAssertionBuilder = otherTypeAssertionBuilder;
    }

    AssertionBuilder<TToType> ISource<TToType>.AssertionBuilder => this;
    
    public InvokableValueAssertionBuilder<TToType> IsTypeOf(Type type)
    {
        return new ValueSource<TToType>(this).IsTypeOf(type);
    }

    public CastableAssertionBuilder<TToType, TExpected> IsTypeOf<TExpected>()
    {
        return new ValueSource<TToType>(this).IsTypeOf<TExpected>();
    }

    public InvokableValueAssertionBuilder<TToType> IsAssignableTo(Type type)
    {
        return new ValueSource<TToType>(this).IsAssignableTo(type);
    }

    public CastableAssertionBuilder<TToType, TExpected> IsAssignableTo<TExpected>()
    {
        return new ValueSource<TToType>(this).IsAssignableTo<TExpected>();
    }

    public InvokableValueAssertionBuilder<TToType> IsAssignableFrom(Type type)
    {
        return new ValueSource<TToType>(this).IsAssignableFrom(type);
    }

    public InvokableValueAssertionBuilder<TToType> IsAssignableFrom<TExpected>()
    {
        return new ValueSource<TToType>(this).IsAssignableFrom<TExpected>();
    }
    
    public InvokableValueAssertionBuilder<TToType> IsNotTypeOf(Type type)
    {
        return new ValueSource<TToType>(this).IsNotTypeOf(type);
    }

    public InvokableValueAssertionBuilder<TToType> IsNotTypeOf<TExpected>()
    {
        return new ValueSource<TToType>(this).IsNotTypeOf<TExpected>();
    }

    public InvokableValueAssertionBuilder<TToType> IsNotAssignableTo(Type type)
    {
        return new ValueSource<TToType>(this).IsNotAssignableTo(type);
    }

    public InvokableValueAssertionBuilder<TToType> IsNotAssignableTo<TExpected>()
    {
        return new ValueSource<TToType>(this).IsNotAssignableTo<TExpected>();
    }

    public InvokableValueAssertionBuilder<TToType> IsNotAssignableFrom(Type type)
    {
        return new ValueSource<TToType>(this).IsNotAssignableFrom(type);
    }

    public InvokableValueAssertionBuilder<TToType> IsNotAssignableFrom<TExpected>()
    {
        return new ValueSource<TToType>(this).IsNotAssignableFrom<TExpected>();
    }
}