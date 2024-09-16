#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TActual> IsEquivalentTo<TActual, TInner, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, IEnumerable<TInner> expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new EnumerableEquivalentToAssertCondition<TActual, TInner, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, equalityComparer));
    }
    
    public static BaseAssertCondition<TActual> IsEmpty<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is)
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new EnumerableCountEqualToAssertCondition<TActual, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), 0));
    }
}