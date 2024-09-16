#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<TActual> IsNotEquivalentTo<TActual, TInner, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot, IEnumerable<TInner> expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new EnumerableNotEquivalentToAssertCondition<TActual, TInner, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, equalityComparer));
    }
    
    public static BaseAssertCondition<TActual> IsNotEmpty<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot)
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new EnumerableCountNotEqualToAssertCondition<TActual, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), 0));
    }
}