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
        return new PropertyEqualsAssertCondition<TActualRootType, TPropertyType, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), selector, expected, true)
            .ChainedTo(assertionBuilder);
    }

    public InvokableAssertionBuilder<TActualRootType, TAnd, TOr> NotEqualTo(TPropertyType expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new PropertyEqualsAssertCondition<TActualRootType, TPropertyType, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), selector, expected, false)
            .ChainedTo(assertionBuilder);
    }
}