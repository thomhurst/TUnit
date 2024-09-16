#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static TOutput IsNotEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return IsNotEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static TOutput IsNotEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return new StringNotEqualsAssertCondition<TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotEmpty<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int,TAnd,TOr>(
            assertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => value != string.Empty,
            (s, _) => $"'{s}' is empty")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotNullOrEmpty<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int, TAnd,TOr>(
            assertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => !string.IsNullOrEmpty(value),
            (s, _) => $"'{s}' is null or empty")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotNullOrWhitespace<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int,TAnd,TOr>(
            assertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => !string.IsNullOrWhiteSpace(value),
            (s, _) => $"'{s}' is null or whitespace")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
}