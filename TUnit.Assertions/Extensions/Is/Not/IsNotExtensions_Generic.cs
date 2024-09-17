#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotNull<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NotNullAssertCondition<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(string.Empty))
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotEqualTo<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NotEqualsAssertCondition<TActual, TAnd, TOr>(
                assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected)
            .ChainedTo(assertionBuilder);
    }

    public static AssertionBuilder<TActual, TAnd, TOr> IsNotTypeOf<TActual, TExpected, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NotTypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(
                assertionBuilder.AppendCallerMethod(typeof(TExpected).FullName))
            .ChainedTo(assertionBuilder);
    }

    public static AssertionBuilder<TActual, TAnd, TOr> IsNotAssignableTo<TActual, TExpected, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => !value!.GetType().IsAssignableTo(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is assignable to {typeof(TExpected).Name}")
            .ChainedTo(assertionBuilder);
    }

    public static AssertionBuilder<TActual, TAnd, TOr> IsNotAssignableFrom<TActual, TExpected, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => !value!.GetType().IsAssignableFrom(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is assignable from {typeof(TExpected).Name}")
            .ChainedTo(assertionBuilder);
    }
}