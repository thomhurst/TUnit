#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static AssertionBuilder<string, TAnd, TOr> DoesNotContain<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return DoesNotContain(assertionBuilder, expected, StringComparison.Ordinal);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> DoesNotContain<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new StringNotContainsAssertCondition<TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> DoesNotStartWith<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return DoesNotStartWith(assertionBuilder, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> DoesNotStartWith<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, string, TAnd, TOr>(
            assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.StartsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does start with \"{expected}\"")
            .ChainedTo(assertionBuilder);
    }
    
        
    public static AssertionBuilder<string, TAnd, TOr> DoesNotEndWith<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return DoesNotEndWith(assertionBuilder, expected, StringComparison.Ordinal);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> DoesNotEndWith<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, string, TAnd, TOr>(
            assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.EndsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does end with \"{expected}\"")
            .ChainedTo(assertionBuilder);
    }
}