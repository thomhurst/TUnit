using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static BaseAssertCondition<TActual> DoesNotContain<TActual, TInner, TAnd, TOr>(this IDoes<TActual, TAnd, TOr> doesNot, TInner expected, IEqualityComparer<TInner?>? equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(doesNot.AssertionConnector, new EnumerableNotContainsAssertCondition<TActual, TInner, TAnd, TOr>(doesNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, equalityComparer));
    }
}