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
    public static InvokableAssertionBuilder<string, TAnd, TOr> IsNotEqualTo<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return IsNotEqualTo(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> IsNotEqualTo<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new StringNotEqualsAssertCondition(expected, stringComparison)
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> IsNotEmpty<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int>(0,
            (value, _, _, _) => value != string.Empty,
            (s, _, _) => $"'{s}' is empty")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> IsNotNullOrEmpty<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int>(0,
            (value, _, _, _) => !string.IsNullOrEmpty(value),
            (s, _, _) => $"'{s}' is null or empty")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> IsNotNullOrWhitespace<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int>(0,
            (value, _, _, _) => !string.IsNullOrWhiteSpace(value),
            (s, _, _) => $"'{s}' is null or whitespace")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
}