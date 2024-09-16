#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<string, TAnd, TOr> IsEqualTo<TAnd, TOr>(this IIs<string, TAnd, TOr> @is, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return IsEqualTo(@is, expected, StringComparison.Ordinal, doNotPopulateThisValue1);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> IsEqualTo<TAnd, TOr>(this IIs<string, TAnd, TOr> @is, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new StringEqualsAssertCondition<TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> IsEmpty<TAnd, TOr>(this IIs<string, TAnd, TOr> @is)
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<string, int,TAnd,TOr>(
            @is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return value == string.Empty;
            },
            (s, _) => $"'{s}' was not empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> IsNullOrEmpty<TAnd, TOr>(this IIs<string, TAnd, TOr> @is)
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<string, int,TAnd,TOr>(
            @is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => string.IsNullOrEmpty(value),
            (s, _) => $"'{s}' is not null or empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> IsNullOrWhitespace<TAnd, TOr>(this IIs<string, TAnd, TOr> @is)
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<string, int,TAnd,TOr>(
            @is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, _) => string.IsNullOrWhiteSpace(value),
            (s, _) => $"'{s}' is not null or whitespace"));
    }
}