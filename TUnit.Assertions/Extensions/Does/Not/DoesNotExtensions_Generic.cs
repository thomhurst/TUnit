﻿using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> Contain<TActual, TInner, TAnd, TOr>(this DoesNot<TActual, TAnd, TOr> doesNot, TInner expected, IEqualityComparer<TInner?>? equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(doesNot, new EnumerableNotContainsAssertCondition<TActual, TInner, TAnd, TOr>(doesNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, equalityComparer));
    }
}