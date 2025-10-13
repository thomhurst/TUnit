#if NET6_0_OR_GREATER
using System.ComponentModel;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DateOnly type using [GenerateAssertion] attributes.
/// These wrap DateOnly checks as extension methods.
/// </summary>
public static partial class DateOnlyAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be today")]
    public static bool IsToday(this DateOnly value) => value == DateOnly.FromDateTime(DateTime.Today);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be today")]
    public static bool IsNotToday(this DateOnly value) => value != DateOnly.FromDateTime(DateTime.Today);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be in a leap year")]
    public static bool IsLeapYear(this DateOnly value) => DateTime.IsLeapYear(value.Year);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be in a leap year")]
    public static bool IsNotLeapYear(this DateOnly value) => !DateTime.IsLeapYear(value.Year);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be on a weekend")]
    public static bool IsOnWeekend(this DateOnly value) => value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek == DayOfWeek.Sunday;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be on a weekday")]
    public static bool IsOnWeekday(this DateOnly value) => value.DayOfWeek != DayOfWeek.Saturday && value.DayOfWeek != DayOfWeek.Sunday;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be in the future")]
    public static bool IsInFuture(this DateOnly value) => value > DateOnly.FromDateTime(DateTime.Today);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be in the past")]
    public static bool IsInPast(this DateOnly value) => value < DateOnly.FromDateTime(DateTime.Today);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be the first day of the month")]
    public static bool IsFirstDayOfMonth(this DateOnly value) => value.Day == 1;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be the last day of the month")]
    public static bool IsLastDayOfMonth(this DateOnly value) => value.Day == DateTime.DaysInMonth(value.Year, value.Month);
}
#endif
