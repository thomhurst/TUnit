using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class Member<TActualRootType, TPropertyType>(IValueSource<TActualRootType> valueSource, Expression<Func<TActualRootType, TPropertyType>> selector)
{
    public InvokableValueAssertionBuilder<TActualRootType> EqualTo(TPropertyType expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new PropertyEqualsExpectedValueAssertCondition<TActualRootType, TPropertyType>(selector, expected, true)
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<TActualRootType> NotEqualTo(TPropertyType expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new PropertyEqualsExpectedValueAssertCondition<TActualRootType, TPropertyType>(selector, expected, false)
            , [doNotPopulateThisValue]);
    }
}