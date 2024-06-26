﻿#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> EqualTo<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new NotEqualsAssertCondition<TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected));
    }
}