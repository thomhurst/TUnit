#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class DoesNotExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> Contain<TActual, TInner, TAnd, TOr>(this DoesNot<TActual, TAnd, TOr> doesNot, TInner expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TActual : IEnumerable<TInner>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return doesNot.Invert(new EnumerableContainsAssertCondition<TActual, TInner, TAnd, TOr>(doesNot.AssertionBuilder.AppendCallerMethod(expectedExpression), expected),
            (_, _) => $"{expected} was found in the collection");
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Contain<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return Contain(doesNot, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Contain<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("stringComparison")] string expression2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return doesNot.Invert(new StringContainsAssertCondition<TAnd, TOr>(doesNot.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), expected, stringComparison),
            (s, _) => $"{expected} was found in {s}");
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> StartWith<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return StartWith(doesNot, expected, StringComparison.Ordinal, expectedExpression);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> StartWith<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("stringComparison")] string expression2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return doesNot.Wrap(new DelegateAssertCondition<string, string, TAnd, TOr>(
            doesNot.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), 
            expected,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.StartsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does start with \"{expected}\""));
    }
    
        
    public static BaseAssertCondition<string, TAnd, TOr> EndWith<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return EndWith(doesNot, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> EndWith<TAnd, TOr>(this DoesNot<string, TAnd, TOr> doesNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("stringComparison")] string expression2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return doesNot.Wrap(new DelegateAssertCondition<string, string, TAnd, TOr>(
            doesNot.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), 
            expected,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return !actual.EndsWith(expected, stringComparison);
            },
            (actual, _) => $"\"{actual}\" does end with \"{expected}\""));
    }
}