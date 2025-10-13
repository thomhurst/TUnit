using System.ComponentModel;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DateTimeOffset type using [GenerateAssertion] attributes.
/// These wrap DateTimeOffset property and method checks as extension methods.
/// </summary>
public static partial class DateTimeOffsetAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be today")]
    public static bool IsToday(this DateTimeOffset value) => value.Date == DateTimeOffset.Now.Date;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be today")]
    public static bool IsNotToday(this DateTimeOffset value) => value.Date != DateTimeOffset.Now.Date;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be UTC")]
    public static bool IsUtc(this DateTimeOffset value) => value.Offset == TimeSpan.Zero;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be UTC")]
    public static bool IsNotUtc(this DateTimeOffset value) => value.Offset != TimeSpan.Zero;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be in a leap year")]
    public static bool IsLeapYear(this DateTimeOffset value) => DateTime.IsLeapYear(value.Year);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be in a leap year")]
    public static bool IsNotLeapYear(this DateTimeOffset value) => !DateTime.IsLeapYear(value.Year);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be in the future")]
    public static bool IsInFuture(this DateTimeOffset value) => value > DateTimeOffset.Now;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be in the past")]
    public static bool IsInPast(this DateTimeOffset value) => value < DateTimeOffset.Now;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be in the future (UTC)")]
    public static bool IsInFutureUtc(this DateTimeOffset value) => value > DateTimeOffset.UtcNow;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be in the past (UTC)")]
    public static bool IsInPastUtc(this DateTimeOffset value) => value < DateTimeOffset.UtcNow;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be on a weekend")]
    public static bool IsOnWeekend(this DateTimeOffset value) => value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek == DayOfWeek.Sunday;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be on a weekday")]
    public static bool IsOnWeekday(this DateTimeOffset value) => value.DayOfWeek != DayOfWeek.Saturday && value.DayOfWeek != DayOfWeek.Sunday;
}
