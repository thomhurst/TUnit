using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class Member<TActualRootType, TPropertyType>(IValueSource<TActualRootType> valueSource, Expression<Func<TActualRootType, TPropertyType>> selector)
{
    private readonly IValueSource<TActualRootType> _valueSource = valueSource;

    public InvokableValueAssertionBuilder<TActualRootType> EqualTo(TPropertyType expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new PropertyEqualsAssertCondition<TActualRootType, TPropertyType>(selector, expected, true)
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<TActualRootType> NotEqualTo(TPropertyType expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new PropertyEqualsAssertCondition<TActualRootType, TPropertyType>(selector, expected, false)
            , [doNotPopulateThisValue]);
    }
}