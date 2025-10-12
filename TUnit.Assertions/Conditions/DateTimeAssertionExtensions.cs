using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DateTime type using [GenerateAssertion] and [AssertionFrom&lt;DateTime&gt;] attributes.
/// These wrap DateTime property and method checks as extension methods.
/// </summary>
[AssertionFrom<DateTime>(nameof(DateTime.IsDaylightSavingTime), ExpectationMessage = "be during daylight saving time")]
[AssertionFrom<DateTime>(nameof(DateTime.IsDaylightSavingTime), CustomName = "IsNotDaylightSavingTime", NegateLogic = true, ExpectationMessage = "be during daylight saving time")]
public static partial class DateTimeAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be today")]
    public static bool IsToday(this DateTime value) => value.Date == DateTime.Today;

    [GenerateAssertion(ExpectationMessage = "to not be today")]
    public static bool IsNotToday(this DateTime value) => value.Date != DateTime.Today;

    [GenerateAssertion(ExpectationMessage = "to be UTC")]
    public static bool IsUtc(this DateTime value) => value.Kind == DateTimeKind.Utc;

    [GenerateAssertion(ExpectationMessage = "to not be UTC")]
    public static bool IsNotUtc(this DateTime value) => value.Kind != DateTimeKind.Utc;

    [GenerateAssertion(ExpectationMessage = "to be in a leap year")]
    public static bool IsLeapYear(this DateTime value) => DateTime.IsLeapYear(value.Year);

    [GenerateAssertion(ExpectationMessage = "to not be in a leap year")]
    public static bool IsNotLeapYear(this DateTime value) => !DateTime.IsLeapYear(value.Year);

    [GenerateAssertion(ExpectationMessage = "to be in the future")]
    public static bool IsInFuture(this DateTime value) => value > DateTime.Now;

    [GenerateAssertion(ExpectationMessage = "to be in the past")]
    public static bool IsInPast(this DateTime value) => value < DateTime.Now;

    [GenerateAssertion(ExpectationMessage = "to be in the future (UTC)")]
    public static bool IsInFutureUtc(this DateTime value) => value > DateTime.UtcNow;

    [GenerateAssertion(ExpectationMessage = "to be in the past (UTC)")]
    public static bool IsInPastUtc(this DateTime value) => value < DateTime.UtcNow;

    [GenerateAssertion(ExpectationMessage = "to be on a weekend")]
    public static bool IsOnWeekend(this DateTime value) => value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek == DayOfWeek.Sunday;

    [GenerateAssertion(ExpectationMessage = "to be on a weekday")]
    public static bool IsOnWeekday(this DateTime value) => value.DayOfWeek != DayOfWeek.Saturday && value.DayOfWeek != DayOfWeek.Sunday;
}
