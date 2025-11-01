#if NET6_0_OR_GREATER
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DateOnly type using [GenerateAssertion(InlineMethodBody = true)] attributes.
/// These wrap DateOnly checks as extension methods.
/// </summary>
file static partial class DateOnlyAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be today", InlineMethodBody = true)]
    public static bool IsToday(this DateOnly value) => value == DateOnly.FromDateTime(DateTime.Today);
    [GenerateAssertion(ExpectationMessage = "to not be today", InlineMethodBody = true)]
    public static bool IsNotToday(this DateOnly value) => value != DateOnly.FromDateTime(DateTime.Today);
    [GenerateAssertion(ExpectationMessage = "to be in a leap year", InlineMethodBody = true)]
    public static bool IsLeapYear(this DateOnly value) => DateTime.IsLeapYear(value.Year);
    [GenerateAssertion(ExpectationMessage = "to not be in a leap year", InlineMethodBody = true)]
    public static bool IsNotLeapYear(this DateOnly value) => !DateTime.IsLeapYear(value.Year);
    [GenerateAssertion(ExpectationMessage = "to be on a weekend", InlineMethodBody = true)]
    public static bool IsOnWeekend(this DateOnly value) => value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek == DayOfWeek.Sunday;
    [GenerateAssertion(ExpectationMessage = "to be on a weekday", InlineMethodBody = true)]
    public static bool IsOnWeekday(this DateOnly value) => value.DayOfWeek != DayOfWeek.Saturday && value.DayOfWeek != DayOfWeek.Sunday;
    [GenerateAssertion(ExpectationMessage = "to be in the future", InlineMethodBody = true)]
    public static bool IsInFuture(this DateOnly value) => value > DateOnly.FromDateTime(DateTime.Today);
    [GenerateAssertion(ExpectationMessage = "to be in the past", InlineMethodBody = true)]
    public static bool IsInPast(this DateOnly value) => value < DateOnly.FromDateTime(DateTime.Today);
    [GenerateAssertion(ExpectationMessage = "to be the first day of the month", InlineMethodBody = true)]
    public static bool IsFirstDayOfMonth(this DateOnly value) => value.Day == 1;
    [GenerateAssertion(ExpectationMessage = "to be the last day of the month", InlineMethodBody = true)]
    public static bool IsLastDayOfMonth(this DateOnly value) => value.Day == DateTime.DaysInMonth(value.Year, value.Month);
}
#endif
