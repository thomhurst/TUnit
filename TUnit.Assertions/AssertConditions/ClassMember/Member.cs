using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class Member<TActualRootType, TPropertyType, TAnd, TOr>(AssertionBuilder<TActualRootType, TAnd, TOr> assertionBuilder, Expression<Func<TActualRootType, TPropertyType>> selector)
    where TAnd : IAnd<TActualRootType, TAnd, TOr>
    where TOr : IOr<TActualRootType, TAnd, TOr>
{
    public InvokableAssertionBuilder<TActualRootType, TAnd, TOr> EqualTo(TPropertyType expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new PropertyEqualsAssertCondition<TActualRootType, TPropertyType, TAnd, TOr>(selector, expected, true)
            .ChainedTo(assertionBuilder, [doNotPopulateThisValue]);
    }

    public InvokableAssertionBuilder<TActualRootType, TAnd, TOr> NotEqualTo(TPropertyType expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new PropertyEqualsAssertCondition<TActualRootType, TPropertyType, TAnd, TOr>(selector, expected, false)
            .ChainedTo(assertionBuilder, [doNotPopulateThisValue]);
    }
}