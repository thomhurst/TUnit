#nullable disable

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;
using TUnit.Assertions.Assertions.Strings.Conditions;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static StringContainsAssertionBuilderWrapper Contains(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return Contains(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static StringContainsAssertionBuilderWrapper Contains(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = null)
    {
        var assertionBuilder = valueSource.RegisterAssertion(new StringContainsExpectedValueAssertCondition(expected, stringComparison)
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
        
        return new StringContainsAssertionBuilderWrapper(assertionBuilder);
    }
    
    public static InvokableValueAssertionBuilder<string> StartsWith(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return StartsWith(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static InvokableValueAssertionBuilder<string> StartsWith(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, string>(expected,
            (actual, _, self) =>
            {
                if (actual is null)
                {
                    self.OverriddenMessage = "Actual string is null";
                    return false;
                }
                
                return actual.StartsWith(expected, stringComparison);
            },
            (actual, _, _) => $"\"{actual}\" does not start with \"{expected}\"",
            $"to start with {expected}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
    
        
    public static InvokableValueAssertionBuilder<string> EndsWith(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return EndsWith(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static InvokableValueAssertionBuilder<string> EndsWith(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, string>(expected,
            (actual, _, _) =>
            {
                Verify.ArgNotNull(actual);
                return actual.EndsWith(expected, stringComparison);
            },
            (actual, _, _) => $"\"{actual}\" does not end with \"{expected}\"",
            $"end with {expected}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
    
    public static InvokableValueAssertionBuilder<string> Matches(this IValueSource<string> valueSource, string regex, [CallerArgumentExpression(nameof(regex))] string expression = "")
    {
        return Matches(valueSource, new Regex(regex), expression);
    }
    
    public static InvokableValueAssertionBuilder<string> Matches(this IValueSource<string> valueSource, Regex regex, [CallerArgumentExpression(nameof(regex))] string expression = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, Regex>(regex,
            (actual, _, _) =>
            {
                Verify.ArgNotNull(actual);
                return regex.IsMatch(actual);
            },
            (actual, _, _) => $"The regex \"{regex}\" does not match with \"{actual}\"",
            $"match {expression}")
            , [expression]);
    }
    
    public static InvokableValueAssertionBuilder<string> DoesNotMatch(this IValueSource<string> valueSource, string regex, [CallerArgumentExpression(nameof(regex))] string expression = "")
    {
        return DoesNotMatch(valueSource, new Regex(regex), expression);
    }
    
    public static InvokableValueAssertionBuilder<string> DoesNotMatch(this IValueSource<string> valueSource, Regex regex, [CallerArgumentExpression(nameof(regex))] string expression = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, Regex>(regex,
                (actual, _, _) =>
                {
                    Verify.ArgNotNull(actual);
                    return !regex.IsMatch(actual);
                },
                (actual, _, _) => $"The regex \"{regex}\" matches with \"{actual}\"",
                $"does not match with {expression}")
            , [expression]);
    }
}