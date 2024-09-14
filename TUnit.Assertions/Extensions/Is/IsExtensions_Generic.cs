#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> IsEqualTo<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new EqualsAssertCondition<TActual, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected));
    }
    
    public static BaseAssertCondition<object, TAnd, TOr> IsEquivalentTo<TAnd, TOr>(this IIs<object, TAnd, TOr> @is, object expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : And<object, TAnd, TOr>, IAnd<object, TAnd, TOr>
        where TOr : Or<object, TAnd, TOr>, IOr<object, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new EquivalentToAssertCondition<object, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected));
    }
}