#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static AssertionBuilder<string, TAnd, TOr> IsNotEqualTo<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return IsNotEqualTo(assertionBuilder, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> IsNotEqualTo<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new StringNotEqualsAssertCondition<TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> IsNotEmpty<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int,TAnd,TOr>(
            assertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => value != string.Empty,
            (s, _) => $"'{s}' is empty")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> IsNotNullOrEmpty<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int, TAnd,TOr>(
            assertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => !string.IsNullOrEmpty(value),
            (s, _) => $"'{s}' is null or empty")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> IsNotNullOrWhitespace<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int,TAnd,TOr>(
            assertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => !string.IsNullOrWhiteSpace(value),
            (s, _) => $"'{s}' is null or whitespace")
            .ChainedTo(assertionBuilder);
    }
}