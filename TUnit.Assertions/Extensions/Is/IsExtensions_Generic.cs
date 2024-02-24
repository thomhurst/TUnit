#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> EqualTo<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new EqualsAssertCondition<TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> AssignableTo<TActual, TExpected, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is) 
        where TActual : TExpected
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            @is.AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, self) => value!.GetType().IsAssignableTo(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is not assignable to {typeof(TExpected).Name}"));
    }
}