#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class StringIsNotExtensions
{
    public static InvokableValueAssertionBuilder<string> IsNotEqualTo(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return IsNotEqualTo(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static InvokableValueAssertionBuilder<string> IsNotEqualTo(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new StringNotEqualsExpectedValueAssertCondition(expected, stringComparison)
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
    
    public static InvokableValueAssertionBuilder<string> IsNotEmpty(this IValueSource<string> valueSource)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(0,
            (value, _, _) => value != string.Empty,
            (s, _, _) => $"'{s}' is empty")
            , []);
    }
    
    public static InvokableValueAssertionBuilder<string> IsNotNullOrEmpty(this IValueSource<string> valueSource)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(0,
            (value, _, _) => !string.IsNullOrEmpty(value),
            (s, _, _) => $"'{s}' is null or empty"), []);
    }
    
    public static InvokableValueAssertionBuilder<string> IsNotNullOrWhitespace(this IValueSource<string> valueSource)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(0,
            (value, _, _) => !string.IsNullOrWhiteSpace(value),
            (s, _, _) => $"'{s}' is null or whitespace")
            , []);
    }
}