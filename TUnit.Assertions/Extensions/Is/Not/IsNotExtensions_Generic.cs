#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> IsNotNull<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is)
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new NotNullAssertCondition<TActual, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(string.Empty)));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> IsNotEqualTo<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(),
            new NotEqualsAssertCondition<TActual, TAnd, TOr>(
                @is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected));
    }

    public static BaseAssertCondition<TActual, TAnd, TOr> IsNotTypeOf<TActual, TExpected, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is)
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(),
            new NotTypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(
                @is.Is().AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName)));
    }

    public static BaseAssertCondition<TActual, TAnd, TOr> IsNotAssignableTo<TActual, TExpected, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is)
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            @is.Is().AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => !value!.GetType().IsAssignableTo(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is assignable to {typeof(TExpected).Name}"));
    }

    public static BaseAssertCondition<TActual, TAnd, TOr> IsNotAssignableFrom<TActual, TExpected, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is)
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            @is.Is().AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => !value!.GetType().IsAssignableFrom(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is assignable from {typeof(TExpected).Name}"));
    }
}