#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> EquivalentTo<TActual, TInner, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot, IEnumerable<TInner> expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new EnumerableNotEquivalentToAssertCondition<TActual, TInner, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, equalityComparer));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Empty<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot)
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new EnumerableCountNotEqualToAssertCondition<TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), 0));
    }
}