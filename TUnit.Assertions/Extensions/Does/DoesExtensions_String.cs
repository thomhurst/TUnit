#nullable disable

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static AssertionBuilder<string, TAnd, TOr> Contains<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return Contains(assertionBuilder, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> Contains<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new StringContainsAssertCondition<TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> StartsWith<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return StartsWith(assertionBuilder, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> StartsWith<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, string, TAnd, TOr>(
            assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
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
            (actual, _) => $"\"{actual}\" does not start with \"{expected}\"")
            .ChainedTo(assertionBuilder);
    }
    
        
    public static AssertionBuilder<string, TAnd, TOr> EndWiths<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return EndWiths(assertionBuilder, expected, StringComparison.Ordinal);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> EndWiths<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, string, TAnd, TOr>(
            assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return actual.EndsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does not end with \"{expected}\"")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> Matches<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string regex, [CallerArgumentExpression("regex")] string expression = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return Matches(assertionBuilder, new Regex(regex), expression);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> Matches<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, Regex regex, [CallerArgumentExpression("regex")] string expression = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, Regex, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(expression), 
            regex,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return regex.IsMatch(actual);
            },
            (actual, _) => $"The regex \"{regex}\" does not match with \"{actual}\"")
            .ChainedTo(assertionBuilder);
    }
}