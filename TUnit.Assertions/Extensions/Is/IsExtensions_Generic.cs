#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> EqualTo<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new EqualsAssertCondition<TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected));
    }
    
    public static BaseAssertCondition<object, TAnd, TOr> EquivalentTo<TAnd, TOr>(this Is<object, TAnd, TOr> @is, object expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : And<object, TAnd, TOr>, IAnd<TAnd, object, TAnd, TOr>
        where TOr : Or<object, TAnd, TOr>, IOr<TOr, object, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new EquivalentToAssertCondition<object, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected));
    }
}