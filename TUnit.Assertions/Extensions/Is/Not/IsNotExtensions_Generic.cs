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
        return new NotNullAssertCondition<TActual, TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(string.Empty))
            .ChainedTo(valueSource.AssertionBuilder);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotEqualTo<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NotEqualsAssertCondition<TActual, TAnd, TOr>(
                valueSource.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected)
            .ChainedTo(valueSource.AssertionBuilder);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotTypeOf<TActual, TExpected, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NotTypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(
                valueSource.AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName))
            .ChainedTo(valueSource.AssertionBuilder);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotAssignableTo<TActual, TExpected, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(default,
            (value, _, _, _) => !value!.GetType().IsAssignableTo(typeof(TExpected)),
            (actual, _, _) => $"{actual?.GetType()} is assignable to {typeof(TExpected).Name}")
            .ChainedTo(valueSource.AssertionBuilder);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotAssignableFrom<TActual, TExpected, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(default,
            (value, _, _, _) => !value!.GetType().IsAssignableFrom(typeof(TExpected)),
            (actual, _, _) => $"{actual?.GetType()} is assignable from {typeof(TExpected).Name}")
            .ChainedTo(valueSource.AssertionBuilder);
    }
}