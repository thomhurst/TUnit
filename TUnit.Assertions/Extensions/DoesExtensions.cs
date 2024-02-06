using System.Text.RegularExpressions;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class DoesExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> Contain<TActual, TInner, TAnd, TOr>(this Does<TActual, TAnd, TOr> does, TInner expected)
        where TActual : IEnumerable<TInner>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return does.Wrap(new EnumerableContainsAssertCondition<TActual, TInner, TAnd, TOr>(does.AssertionBuilder, expected));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Contain<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return Contain(does, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Contain<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected, StringComparison stringComparison)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return does.Wrap(new StringContainsAssertCondition<TAnd, TOr>(does.AssertionBuilder, expected, stringComparison));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> StartWith<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return StartWith(does, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> StartWith<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected, StringComparison stringComparison)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return does.Wrap(new DelegateAssertCondition<string, string, TAnd, TOr>(
            does.AssertionBuilder, 
            expected,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return actual.StartsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does not start with \"{expected}\""));
    }
    
        
    public static BaseAssertCondition<string, TAnd, TOr> EndWith<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return EndWith(does, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> EndWith<TAnd, TOr>(this Does<string, TAnd, TOr> does, string expected, StringComparison stringComparison)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return does.Wrap(new DelegateAssertCondition<string, string, TAnd, TOr>(
            does.AssertionBuilder, 
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
    
    public static BaseAssertCondition<string, TAnd, TOr> Match<TAnd, TOr>(this Does<string, TAnd, TOr> does, Regex regex)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return does.Wrap(new DelegateAssertCondition<string, Regex, TAnd, TOr>(
            does.AssertionBuilder, 
            regex,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return regex.IsMatch(actual);
            },
            (actual, _) => $"The regex \"{regex}\" does not match with \"{actual}\""));
    }
}