#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<string, TAnd, TOr> EqualTo<TAnd, TOr>(this IsNot<string, TAnd, TOr> isNot, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return EqualTo(isNot, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> EqualTo<TAnd, TOr>(this IsNot<string, TAnd, TOr> isNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return isNot.Wrap(new StringNotEqualsAssertCondition<TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison));
    }
}