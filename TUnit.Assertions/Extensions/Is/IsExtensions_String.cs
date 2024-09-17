﻿#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static AssertionBuilder<string, TAnd, TOr> IsEqualTo<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return IsEqualTo(assertionBuilder, expected, StringComparison.Ordinal, doNotPopulateThisValue1);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> IsEqualTo<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new StringEqualsAssertCondition<TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<string, TAnd, TOr> IsEmpty<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int,TAnd,TOr>(
            assertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return value == string.Empty;
            },
            (s, _) => $"'{s}' was not empty")
            .ChainedTo(assertionBuilder); }
    
    public static AssertionBuilder<string, TAnd, TOr> IsNullOrEmpty<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int,TAnd,TOr>(
            assertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => string.IsNullOrEmpty(value),
            (s, _) => $"'{s}' is not null or empty")
            .ChainedTo(assertionBuilder); }
    
    public static AssertionBuilder<string, TAnd, TOr> IsNullOrWhitespace<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int,TAnd,TOr>(
            assertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => string.IsNullOrWhiteSpace(value),
            (s, _) => $"'{s}' is not null or whitespace")
            .ChainedTo(assertionBuilder); }
}