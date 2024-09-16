#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static TOutput DoesNotContain<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return DoesNotContain<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, expected, StringComparison.Ordinal);
    }
    
    public static TOutput DoesNotContain<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return new StringNotContainsAssertCondition<TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput DoesNotStartWith<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return DoesNotStartWith<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static TOutput DoesNotStartWith<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
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
                return !actual.StartsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does start with \"{expected}\"")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
        
    public static TOutput DoesNotEndWith<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return DoesNotEndWith<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, expected, StringComparison.Ordinal);
    }
    
    public static TOutput DoesNotEndWith<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
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
                return !actual.EndsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does end with \"{expected}\"")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
}