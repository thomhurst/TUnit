using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class Member<TActualRootType, TPropertyType>(AssertionBuilder<TActualRootType> assertionBuilder, Expression<Func<TActualRootType, TPropertyType>> selector)
{
    public InvokableValueAssertionBuilder<TActualRootType> EqualTo(TPropertyType expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return (InvokableValueAssertionBuilder<TActualRootType>)new PropertyEqualsAssertCondition<TActualRootType, TPropertyType>(selector, expected, true)
            .ChainedTo(assertionBuilder, [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<TActualRootType> NotEqualTo(TPropertyType expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return (InvokableValueAssertionBuilder<TActualRootType>)new PropertyEqualsAssertCondition<TActualRootType, TPropertyType>(selector, expected, false)
            .ChainedTo(assertionBuilder, [doNotPopulateThisValue]);
    }
}