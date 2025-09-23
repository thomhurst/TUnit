#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Exceptions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static AssertionBuilder<TActual> HasMessageEqualTo<TActual>(this IValueSource<TActual> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null) where TActual : Exception
    {
        return HasMessageEqualTo(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public static AssertionBuilder<TActual> HasMessageEqualTo<TActual>(this IValueSource<TActual> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = null) where TActual : Exception
    {
        return valueSource.RegisterAssertion(new ExceptionMessageEqualsExpectedValueAssertCondition<TActual>(expected, stringComparison),
            [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }

    public static AssertionBuilder<TActual> HasMessageContaining<TActual>(this IValueSource<TActual> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null) where TActual : Exception
    {
        return HasMessageContaining(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public static AssertionBuilder<TActual> HasMessageContaining<TActual>(this IValueSource<TActual> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = null) where TActual : Exception
    {
        return valueSource.RegisterAssertion(new ExceptionMessageContainingExpectedValueAssertCondition<TActual>(expected, stringComparison),
            [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }

    public static AssertionBuilder<TActual> HasMessageStartingWith<TActual>(this IValueSource<TActual> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null) where TActual : Exception
    {
        return HasMessageStartingWith(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public static AssertionBuilder<TActual> HasMessageStartingWith<TActual>(this IValueSource<TActual> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = null) where TActual : Exception
    {
        return valueSource.RegisterAssertion(new ExceptionMessageStartingWithExpectedValueAssertCondition<TActual>(expected, stringComparison),
            [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }

    public static AssertionBuilder<TActual> HasMessageEndingWith<TActual>(this IValueSource<TActual> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null) where TActual : Exception
    {
        return HasMessageEndingWith(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public static AssertionBuilder<TActual> HasMessageEndingWith<TActual>(this IValueSource<TActual> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = null) where TActual : Exception
    {
        return valueSource.RegisterAssertion(new ExceptionMessageEndingWithExpectedValueAssertCondition<TActual>(expected, stringComparison),
            [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }

    public static AssertionBuilder<TActual> HasMessageMatching<TActual>(this IValueSource<TActual> valueSource, StringMatcher expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = null) where TActual : Exception
    {
        return valueSource.RegisterAssertion(new ExceptionMessageMatchingExpectedAssertCondition<TActual>(expected),
            [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
}
