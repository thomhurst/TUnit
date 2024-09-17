﻿#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static AssertionBuilder<TActual, TAnd, TOr> Contains<TActual, TInner, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TInner expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableContainsAssertCondition<TActual, TInner, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, equalityComparer)
            .ChainedTo(assertionBuilder);
    }
}