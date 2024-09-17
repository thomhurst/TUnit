#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableAssertionBuilder<string, TAnd, TOr> IsEqualTo<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return IsEqualTo(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue1);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> IsEqualTo<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new StringEqualsAssertCondition<TAnd, TOr>(expected, stringComparison)
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> IsEmpty<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int,TAnd,TOr>(0,
            (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _, _) => "Actual string is null");
                    return false;
                }
                
                return value == string.Empty;
            },
            (s, _, _) => $"'{s}' was not empty")
            .ChainedTo(valueSource.AssertionBuilder, []); }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> IsNullOrEmpty<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int,TAnd,TOr>(0,
            (value, _, _, _) => string.IsNullOrEmpty(value),
            (s, _, _) => $"'{s}' is not null or empty")
            .ChainedTo(valueSource.AssertionBuilder, []); }
    
    public static InvokableAssertionBuilder<string, TAnd, TOr> IsNullOrWhitespace<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new DelegateAssertCondition<string, int,TAnd,TOr>(0,
            (value, _, _, _) => string.IsNullOrWhiteSpace(value),
            (s, _, _) => $"'{s}' is not null or whitespace")
            .ChainedTo(valueSource.AssertionBuilder, []); }
}