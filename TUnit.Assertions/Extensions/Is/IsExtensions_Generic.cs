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
    
    public static BaseAssertCondition<object, TAnd, TOr> IsSameReference<TAnd, TOr>(this IIs<object, TAnd, TOr> @is, object expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : And<object, TAnd, TOr>, IAnd<object, TAnd, TOr>
        where TOr : Or<object, TAnd, TOr>, IOr<object, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new SameReferenceAssertCondition<object, object, TAnd,TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> IsNull<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is)
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new NullAssertCondition<TActual, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(string.Empty)));
    }

    public static BaseAssertCondition<TActual, TAnd, TOr> IsTypeOf<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, Type type) where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), 
            new TypeOfAssertCondition<TActual, TAnd, TOr>(
                @is.Is().AssertionBuilder.AppendCallerMethod(type.FullName), type));
    }

    public static BaseAssertCondition<TActual, TAnd, TOr> IsAssignableTo<TActual, TExpected, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is) 
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            @is.Is().AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => value!.GetType().IsAssignableTo(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is not assignable to {typeof(TExpected).Name}"));
    }

    public static BaseAssertCondition<TActual, TAnd, TOr> IsAssignableFrom<TActual, TExpected, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is) 
        where TExpected : TActual
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            @is.Is().AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => value!.GetType().IsAssignableFrom(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is not assignable from {typeof(TExpected).Name}"));
    }
}