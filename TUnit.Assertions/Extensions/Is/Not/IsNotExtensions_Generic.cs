#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> IsNotEqualTo<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.IsNot(), new NotEqualsAssertCondition<TActual, TAnd, TOr>(isNot.IsNot().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected));
    }
}