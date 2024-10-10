#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;

namespace TUnit.Assertions.Extensions;

public static class StringIsExtensions
{
    public static StringEqualToAssertionBuilderWrapper IsEqualTo(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
    {
        return IsEqualTo(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue1);
    }
    
    public static StringEqualToAssertionBuilderWrapper IsEqualTo(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        var assertionBuilder = valueSource.RegisterAssertion(new StringEqualsExpectedValueAssertCondition(expected, stringComparison)
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
        
        return new StringEqualToAssertionBuilderWrapper(assertionBuilder);
    }
    
    public static InvokableValueAssertionBuilder<string> IsEmpty(this IValueSource<string> valueSource)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(0,
            (value, _, self) =>
            {
                if (value is null)
                {
                    self.FailWithMessage("Actual string is null");
                    return false;
                }
                
                return value == string.Empty;
            },
            (s, _, _) => $"'{s}' was not empty")
            , []); }
    
    public static InvokableValueAssertionBuilder<string> IsNullOrEmpty(this IValueSource<string> valueSource)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(0,
            (value, _, _) => string.IsNullOrEmpty(value),
            (s, _, _) => $"'{s}' is not null or empty")
            , []); }
    
    public static InvokableValueAssertionBuilder<string> IsNullOrWhitespace(this IValueSource<string> valueSource)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(0,
            (value, _, _) => string.IsNullOrWhiteSpace(value),
            (s, _, _) => $"'{s}' is not null or whitespace")
            , []); }
}