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
        return AssertionConditionCombiner.Combine(isNot, new StringNotEqualsAssertCondition<TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Empty<TAnd, TOr>(this IsNot<string, TAnd, TOr> isNot)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new DelegateAssertCondition<string, int,TAnd,TOr>(
            isNot.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) => value != string.Empty,
            (s, _) => $"'{s}' is empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> NullOrEmpty<TAnd, TOr>(this IsNot<string, TAnd, TOr> isNot)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new DelegateAssertCondition<string, int, TAnd,TOr>(
            isNot.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) => !string.IsNullOrEmpty(value),
            (s, _) => $"'{s}' is null or empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> NullOrWhitespace<TAnd, TOr>(this IsNot<string, TAnd, TOr> isNot)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new DelegateAssertCondition<string, int,TAnd,TOr>(
            isNot.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) => !string.IsNullOrWhiteSpace(value),
            (s, _) => $"'{s}' is null or whitespace"));
    }
}