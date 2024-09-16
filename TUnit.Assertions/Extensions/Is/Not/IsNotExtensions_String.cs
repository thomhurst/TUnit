#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<string, TAnd, TOr> IsNotEqualTo<TAnd, TOr>(this IIs<string, TAnd, TOr> isNot, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return IsNotEqualTo(isNot, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> IsNotEqualTo<TAnd, TOr>(this IIs<string, TAnd, TOr> isNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new StringNotEqualsAssertCondition<TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> IsNotEmpty<TAnd, TOr>(this IIs<string, TAnd, TOr> isNot)
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<string, int,TAnd,TOr>(
            isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => value != string.Empty,
            (s, _) => $"'{s}' is empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> IsNotNullOrEmpty<TAnd, TOr>(this IIs<string, TAnd, TOr> isNot)
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<string, int, TAnd,TOr>(
            isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => !string.IsNullOrEmpty(value),
            (s, _) => $"'{s}' is null or empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> IsNotNullOrWhitespace<TAnd, TOr>(this IIs<string, TAnd, TOr> isNot)
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<string, int,TAnd,TOr>(
            isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => !string.IsNullOrWhiteSpace(value),
            (s, _) => $"'{s}' is null or whitespace"));
    }
}