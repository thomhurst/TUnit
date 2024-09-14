﻿#nullable disable

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static BaseAssertCondition<string, TAnd, TOr> Contains<TAnd, TOr>(this IDoes<string, TAnd, TOr> does, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return Contains(does, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Contains<TAnd, TOr>(this IDoes<string, TAnd, TOr> does, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(does.Does(), new StringContainsAssertCondition<TAnd, TOr>(does.Does().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> StartsWith<TAnd, TOr>(this IDoes<string, TAnd, TOr> does, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return StartsWith(does, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> StartsWith<TAnd, TOr>(this IDoes<string, TAnd, TOr> does, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(does.Does(), new DelegateAssertCondition<string, string, TAnd, TOr>(
            does.Does().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, self) =>
            {
                if (actual is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return actual.StartsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does not start with \"{expected}\""));
    }
    
        
    public static BaseAssertCondition<string, TAnd, TOr> EndWiths<TAnd, TOr>(this IDoes<string, TAnd, TOr> does, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return EndWiths(does, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> EndWiths<TAnd, TOr>(this IDoes<string, TAnd, TOr> does, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(does.Does(), new DelegateAssertCondition<string, string, TAnd, TOr>(
            does.Does().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return actual.EndsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does not end with \"{expected}\""));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Matches<TAnd, TOr>(this IDoes<string, TAnd, TOr> does, string regex, [CallerArgumentExpression("regex")] string expression = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return Matches(does, new Regex(regex), expression);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Matches<TAnd, TOr>(this IDoes<string, TAnd, TOr> does, Regex regex, [CallerArgumentExpression("regex")] string expression = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(does.Does(), new DelegateAssertCondition<string, Regex, TAnd, TOr>(
            does.Does().AssertionBuilder.AppendCallerMethod(expression), 
            regex,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return regex.IsMatch(actual);
            },
            (actual, _) => $"The regex \"{regex}\" does not match with \"{actual}\""));
    }
}