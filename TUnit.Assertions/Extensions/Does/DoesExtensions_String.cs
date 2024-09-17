#nullable disable

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static InvokableAssertionBuilder<string, TAnd, TOr> Contains<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return Contains(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> Contains<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new StringContainsAssertCondition<TAnd, TOr>(expected, stringComparison)
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> StartsWith<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return StartsWith(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> StartsWith<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, string, TAnd, TOr>(expected,
            (actual, _, _, self) =>
            {
                if (actual is null)
                {
                    self.WithMessage((_, _, _) => "Actual string is null");
                    return false;
                }
                
                return actual.StartsWith(expected, stringComparison);
            },
            (actual, _, _) => $"\"{actual}\" does not start with \"{expected}\"")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
    
        
    public static InvokableAssertionBuilder<string, TAnd, TOr> EndWiths<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return EndWiths(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> EndWiths<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, string, TAnd, TOr>(expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return actual.EndsWith(expected, stringComparison);
            },
            (actual, _, _) => $"\"{actual}\" does not end with \"{expected}\"")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> Matches<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string regex, [CallerArgumentExpression("regex")] string expression = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return Matches(valueSource, new Regex(regex), expression);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> Matches<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, Regex regex, [CallerArgumentExpression("regex")] string expression = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, Regex, TAnd, TOr>(regex,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return regex.IsMatch(actual);
            },
            (actual, _, _) => $"The regex \"{regex}\" does not match with \"{actual}\"")
            .ChainedTo(valueSource.AssertionBuilder, [expression]);
    }
}