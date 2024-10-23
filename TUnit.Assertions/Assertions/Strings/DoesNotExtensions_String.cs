#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static InvokableValueAssertionBuilder<string> DoesNotContain(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return DoesNotContain(valueSource, expected, StringComparison.Ordinal);
    }
    
    public static InvokableValueAssertionBuilder<string> DoesNotContain(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "", [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new StringNotContainsExpectedValueAssertCondition(expected, stringComparison)
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
    
    public static InvokableValueAssertionBuilder<string> DoesNotStartWith(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return DoesNotStartWith(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static InvokableValueAssertionBuilder<string> DoesNotStartWith(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "", [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, string>(expected,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.StartsWith(expected, stringComparison);
            },
            (actual, _, _) => $"\"{actual}\" does start with \"{expected}\"",
            $"not start with {expected}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
    
        
    public static InvokableValueAssertionBuilder<string> DoesNotEndWith(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return DoesNotEndWith(valueSource, expected, StringComparison.Ordinal);
    }
    
    public static InvokableValueAssertionBuilder<string> DoesNotEndWith(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "", [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, string>(expected,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.EndsWith(expected, stringComparison);
            },
            (actual, _, _) => $"\"{actual}\" does end with \"{expected}\"",
            $"not end with {expected}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
}