using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// DateTime-specific assertion extension methods.
/// </summary>
public static class DateTimeAssertionExtensions
{
    /// <summary>
    /// Asserts that the DateTime value represents today's date (ignoring time component).
    /// </summary>
    public static IsTodayAssertion IsToday(
        this IAssertionSource<DateTime> source)
    {
        source.Context.ExpressionBuilder.Append(".IsToday()");
        return new IsTodayAssertion(source.Context);
    }

    /// <summary>
    /// Asserts that the DateTime value does not represent today's date.
    /// </summary>
    public static IsNotTodayAssertion IsNotToday(
        this IAssertionSource<DateTime> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotToday()");
        return new IsNotTodayAssertion(source.Context);
    }

    /// <summary>
    /// Asserts that the DateTime value is in UTC.
    /// </summary>
    public static IsUtcAssertion IsUtc(
        this IAssertionSource<DateTime> source)
    {
        source.Context.ExpressionBuilder.Append(".IsUtc()");
        return new IsUtcAssertion(source.Context);
    }

    /// <summary>
    /// Asserts that the DateTime value is not in UTC.
    /// </summary>
    public static IsNotUtcAssertion IsNotUtc(
        this IAssertionSource<DateTime> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotUtc()");
        return new IsNotUtcAssertion(source.Context);
    }

    /// <summary>
    /// Asserts that the DateTime value is in a leap year.
    /// </summary>
    public static IsLeapYearAssertion IsLeapYear(
        this IAssertionSource<DateTime> source)
    {
        source.Context.ExpressionBuilder.Append(".IsLeapYear()");
        return new IsLeapYearAssertion(source.Context);
    }

    /// <summary>
    /// Asserts that the DateTime value is not in a leap year.
    /// </summary>
    public static IsNotLeapYearAssertion IsNotLeapYear(
        this IAssertionSource<DateTime> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotLeapYear()");
        return new IsNotLeapYearAssertion(source.Context);
    }

    /// <summary>
    /// Asserts that the DateTime value is during daylight saving time.
    /// </summary>
    public static IsDaylightSavingTimeAssertion IsDaylightSavingTime(
        this IAssertionSource<DateTime> source)
    {
        source.Context.ExpressionBuilder.Append(".IsDaylightSavingTime()");
        return new IsDaylightSavingTimeAssertion(source.Context);
    }

    /// <summary>
    /// Asserts that the DateTime value is not during daylight saving time.
    /// </summary>
    public static IsNotDaylightSavingTimeAssertion IsNotDaylightSavingTime(
        this IAssertionSource<DateTime> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotDaylightSavingTime()");
        return new IsNotDaylightSavingTimeAssertion(source.Context);
    }

    /// <summary>
    /// Asserts that the DateTime exactly equals the expected value (including ticks).
    /// Use this instead of IsEqualTo when you need exact equality without tolerance.
    /// </summary>
    public static DateTimeEqualsExactAssertion EqualsExact(
        this IAssertionSource<DateTime> source,
        DateTime expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".EqualsExact({expression})");
        return new DateTimeEqualsExactAssertion(source.Context, expected);
    }
}
