#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions.Is.Not;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> EqualTo<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new NotEqualsAssertCondition<TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected));
    }
}