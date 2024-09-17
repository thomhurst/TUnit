﻿#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsEqualTo<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EqualsAssertCondition<TActual>(expected)
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1]);
    }
    
    public static InvokableAssertionBuilder<object, TAnd, TOr> IsEquivalentTo<TAnd, TOr>(this IValueSource<object, TAnd, TOr> valueSource, object expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<object, TAnd, TOr>
        where TOr : IOr<object, TAnd, TOr>
    {
        return new EquivalentToAssertCondition<object>(expected)
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1]);
    }
    
    public static InvokableAssertionBuilder<object, TAnd, TOr> IsSameReference<TAnd, TOr>(this IValueSource<object, TAnd, TOr> valueSource, object expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<object, TAnd, TOr>
        where TOr : IOr<object, TAnd, TOr>
    {
        return new SameReferenceAssertCondition<object, object>(expected)
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1]);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNull<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NullAssertCondition<TActual>()
            .ChainedTo(valueSource.AssertionBuilder, []);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsTypeOf<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, Type type) where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new TypeOfAssertCondition<TActual>(type)
            .ChainedTo(valueSource.AssertionBuilder, [type.Name]);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsAssignableTo<TActual, TExpected, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource) 
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TExpected>(default,
            (value, _, _, _) => value!.GetType().IsAssignableTo(typeof(TExpected)),
            (actual, _, _) => $"{actual?.GetType()} is not assignable to {typeof(TExpected).Name}")
            .ChainedTo(valueSource.AssertionBuilder, [typeof(TExpected).Name]); }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsAssignableFrom<TActual, TExpected, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource) 
        where TExpected : TActual
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TExpected>(default,
            (value, _, _, _) => value!.GetType().IsAssignableFrom(typeof(TExpected)),
            (actual, _, _) => $"{actual?.GetType()} is not assignable from {typeof(TExpected).Name}")
            .ChainedTo(valueSource.AssertionBuilder, [typeof(TExpected).Name]); }
}