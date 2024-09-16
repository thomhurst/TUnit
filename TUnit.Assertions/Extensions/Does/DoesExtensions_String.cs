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
    public static TOutput Contains<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return Contains<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static TOutput Contains<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return new StringContainsAssertCondition<TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput StartsWith<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return StartsWith<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static TOutput StartsWith<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
        
    public static TOutput EndWiths<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return EndWiths<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, expected, StringComparison.Ordinal);
    }
    
    public static TOutput EndWiths<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput Matches<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string regex, [CallerArgumentExpression("regex")] string expression = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return Matches<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, new Regex(regex), expression);
    }
    
    public static TOutput Matches<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, Regex regex, [CallerArgumentExpression("regex")] string expression = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
}