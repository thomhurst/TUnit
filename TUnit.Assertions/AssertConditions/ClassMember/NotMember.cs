using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class NotMember<TActualRootType, TPropertyType, TAnd, TOr>(Connector<TActualRootType, TAnd, TOr> connector, AssertionBuilder<TActualRootType> assertionBuilder, Expression<Func<TActualRootType, TPropertyType>> selector)
    where TAnd : And<TActualRootType, TAnd, TOr>, IAnd<TAnd, TActualRootType, TAnd, TOr>
    where TOr : Or<TActualRootType, TAnd, TOr>, IOr<TOr, TActualRootType, TAnd, TOr>
{
    public BaseAssertCondition<TActualRootType, TAnd, TOr> EqualTo(TPropertyType expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return AssertionConditionCombiner.Combine(connector, new PropertyEqualsAssertCondition<TActualRootType, TPropertyType, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), selector, expected, false));
    }
}