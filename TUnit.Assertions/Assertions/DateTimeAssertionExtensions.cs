using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion(typeof(DateTime), typeof(DateTime), nameof(DateTime.IsDaylightSavingTime))]
[CreateAssertion(typeof(DateTime), typeof(DateTime), nameof(DateTime.IsDaylightSavingTime), CustomName = "IsNotDaylightSavingTime", NegateLogic = true)]

[CreateAssertion(typeof(DateTimeOffset), typeof(DateTimeOffset), nameof(DateTimeOffset.EqualsExact))]

// Common DateTime comparison patterns via static methods
[CreateAssertion(typeof(DateTime), typeof(DateTimeAssertionExtensions), nameof(IsToday))]
[CreateAssertion(typeof(DateTime), typeof(DateTimeAssertionExtensions), nameof(IsToday), CustomName = "IsNotToday", NegateLogic = true)]

[CreateAssertion(typeof(DateTime), typeof(DateTimeAssertionExtensions), nameof(IsWeekend))]
[CreateAssertion(typeof(DateTime), typeof(DateTimeAssertionExtensions), nameof(IsWeekend), CustomName = "IsWeekday", NegateLogic = true)]

[CreateAssertion(typeof(DateTime), typeof(DateTimeAssertionExtensions), nameof(IsLeapYear))]
[CreateAssertion(typeof(DateTime), typeof(DateTimeAssertionExtensions), nameof(IsLeapYear), CustomName = "IsNotLeapYear", NegateLogic = true)]

[CreateAssertion(typeof(DateTime), typeof(DateTimeAssertionExtensions), nameof(IsUtc))]
[CreateAssertion(typeof(DateTime), typeof(DateTimeAssertionExtensions), nameof(IsUtc), CustomName = "IsNotUtc", NegateLogic = true)]
public static partial class DateTimeAssertionExtensions
{
    // Helper methods for DateTime assertions
    internal static bool IsToday(DateTime dateTime) => dateTime.Date == DateTime.Today;

    internal static bool IsWeekday(DateTime dateTime) => !IsWeekend(dateTime);

    internal static bool IsWeekend(DateTime dateTime) =>
        dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    internal static bool IsLeapYear(DateTime dateTime) => DateTime.IsLeapYear(dateTime.Year);

    internal static bool IsUtc(DateTime dateTime) => dateTime.Kind == DateTimeKind.Utc;
}
