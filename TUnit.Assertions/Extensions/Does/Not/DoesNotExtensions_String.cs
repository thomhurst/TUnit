#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static BaseAssertCondition<string, TAnd, TOr> DoesNotContain<TAnd, TOr>(this IDoes<string, TAnd, TOr> doesNot, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return DoesNotContain(doesNot, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> DoesNotContain<TAnd, TOr>(this IDoes<string, TAnd, TOr> doesNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(doesNot.DoesNot(), new StringNotContainsAssertCondition<TAnd, TOr>(doesNot.DoesNot().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> DoesNotStartWith<TAnd, TOr>(this IDoes<string, TAnd, TOr> doesNot, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return DoesNotStartWith(doesNot, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> DoesNotStartWith<TAnd, TOr>(this IDoes<string, TAnd, TOr> doesNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(doesNot.DoesNot(), new DelegateAssertCondition<string, string, TAnd, TOr>(
            doesNot.DoesNot().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.StartsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does start with \"{expected}\""));
    }
    
        
    public static BaseAssertCondition<string, TAnd, TOr> DoesNotEndWith<TAnd, TOr>(this IDoes<string, TAnd, TOr> doesNot, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return DoesNotEndWith(doesNot, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> DoesNotEndWith<TAnd, TOr>(this IDoes<string, TAnd, TOr> doesNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(doesNot.DoesNot(), new DelegateAssertCondition<string, string, TAnd, TOr>(
            doesNot.DoesNot().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.EndsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does end with \"{expected}\""));
    }
}