#nullable disable

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class DoesExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> Contain<TActual, TInner, TAnd, TOr>(this Does<TActual, TAnd, TOr> does, TInner expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TActual : IEnumerable<TInner>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return does.Wrap(new EnumerableContainsAssertCondition<TActual, TInner, TAnd, TOr>(does.AssertionBuilder.AppendCallerMethod(expectedExpression), expected));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Contain<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return Contain(does, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Contain<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("stringComparison")] string expression2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return does.Wrap(new StringContainsAssertCondition<TAnd, TOr>(does.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), expected, stringComparison));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> StartWith<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return StartWith(does, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> StartWith<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("stringComparison")] string expression2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return does.Wrap(new DelegateAssertCondition<string, string, TAnd, TOr>(
            does.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), 
            expected,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return actual.StartsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does not start with \"{expected}\""));
    }
    
        
    public static BaseAssertCondition<string, TAnd, TOr> EndWith<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return EndWith(does, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> EndWith<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("stringComparison")] string expression2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return does.Wrap(new DelegateAssertCondition<string, string, TAnd, TOr>(
            does.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), 
            expected,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return actual.EndsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does not end with \"{expected}\""));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Match<TAnd, TOr>(this Does<string, TAnd, TOr> does, string regex)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return Match(does, new Regex(regex));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Match<TAnd, TOr>(this Does<string, TAnd, TOr> does, Regex regex, [CallerArgumentExpression("regex")] string expression = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return does.Wrap(new DelegateAssertCondition<string, Regex, TAnd, TOr>(
            does.AssertionBuilder.AppendCallerMethod(expression), 
            regex,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return regex.IsMatch(actual);
            },
            (actual, _) => $"The regex \"{regex}\" does not match with \"{actual}\""));
    }
}