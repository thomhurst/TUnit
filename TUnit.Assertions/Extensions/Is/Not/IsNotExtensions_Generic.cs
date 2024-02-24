#nullable disable

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
        return isNot.Wrap(new NotEqualsAssertCondition<TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> AssignableTo<TActual, TExpected, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot) 
        where TActual : TExpected
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            isNot.AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, self) => !value!.GetType().IsAssignableTo(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is assignable to {typeof(TExpected).Name}"));
    }
}