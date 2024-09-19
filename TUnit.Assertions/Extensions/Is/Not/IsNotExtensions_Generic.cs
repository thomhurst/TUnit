#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotNull<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NotNullAssertCondition<TActual>()
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotEqualTo<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NotEqualsAssertCondition<TActual>(expected)
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotTypeOf<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, Type type)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, Type>(default,
                (value, _, _, _) => value!.GetType() != type,
                (actual, _, _) => $"{actual?.GetType()} is type of {type.Name}")
            .ChainedTo(valueSource.AssertionBuilder, [type.Name]);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotAssignableTo<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, Type type)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, Type>(default,
            (value, _, _, _) => !value!.GetType().IsAssignableTo(type),
            (actual, _, _) => $"{actual?.GetType()} is assignable to {type.Name}")
            .ChainedTo(valueSource.AssertionBuilder, [type.Name]);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotAssignableFrom<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, Type type)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, Type>(default,
            (value, _, _, _) => !value!.GetType().IsAssignableFrom(type),
            (actual, _, _) => $"{actual?.GetType()} is assignable from {type.Name}")
            .ChainedTo(valueSource.AssertionBuilder, [type.Name]);
    }
}