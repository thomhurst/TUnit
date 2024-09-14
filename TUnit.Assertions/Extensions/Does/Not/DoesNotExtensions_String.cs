﻿#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static BaseAssertCondition<string, TAnd, TOr> Contain<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return Contain(doesNot, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Contain<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(doesNot, new StringNotContainsAssertCondition<TAnd, TOr>(doesNot.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> StartWith<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return StartWith(doesNot, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> StartWith<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(doesNot, new DelegateAssertCondition<string, string, TAnd, TOr>(
            doesNot.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.StartsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does start with \"{expected}\""));
    }
    
        
    public static BaseAssertCondition<string, TAnd, TOr> EndWith<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return EndWith(doesNot, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> EndWith<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(doesNot, new DelegateAssertCondition<string, string, TAnd, TOr>(
            doesNot.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.EndsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does end with \"{expected}\""));
    }
}