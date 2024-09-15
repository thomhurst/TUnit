#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> Contains<TActual, TInner, TAnd, TOr>(this IDoes<TActual, TAnd, TOr> does, TInner expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(does.Does(), new EnumerableContainsAssertCondition<TActual, TInner, TAnd, TOr>(does.Does().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, equalityComparer));
    }
}