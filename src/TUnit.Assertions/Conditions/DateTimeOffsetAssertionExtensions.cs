using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DateTimeOffset type using [GenerateAssertion(InlineMethodBody = true)] attributes.
/// These wrap DateTimeOffset property and method checks as extension methods.
/// </summary>
file static partial class DateTimeOffsetAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be today", InlineMethodBody = true)]
    public static bool IsToday(this DateTimeOffset value) => value.Date == DateTimeOffset.Now.Date;
    [GenerateAssertion(ExpectationMessage = "to not be today", InlineMethodBody = true)]
    public static bool IsNotToday(this DateTimeOffset value) => value.Date != DateTimeOffset.Now.Date;
    [GenerateAssertion(ExpectationMessage = "to be UTC", InlineMethodBody = true)]
    public static bool IsUtc(this DateTimeOffset value) => value.Offset == TimeSpan.Zero;
    [GenerateAssertion(ExpectationMessage = "to not be UTC", InlineMethodBody = true)]
    public static bool IsNotUtc(this DateTimeOffset value) => value.Offset != TimeSpan.Zero;
    [GenerateAssertion(ExpectationMessage = "to be in a leap year", InlineMethodBody = true)]
    public static bool IsLeapYear(this DateTimeOffset value) => DateTime.IsLeapYear(value.Year);
    [GenerateAssertion(ExpectationMessage = "to not be in a leap year", InlineMethodBody = true)]
    public static bool IsNotLeapYear(this DateTimeOffset value) => !DateTime.IsLeapYear(value.Year);
    [GenerateAssertion(ExpectationMessage = "to be in the future", InlineMethodBody = true)]
    public static bool IsInFuture(this DateTimeOffset value) => value > DateTimeOffset.Now;
    [GenerateAssertion(ExpectationMessage = "to be in the past", InlineMethodBody = true)]
    public static bool IsInPast(this DateTimeOffset value) => value < DateTimeOffset.Now;
    [GenerateAssertion(ExpectationMessage = "to be in the future (UTC)", InlineMethodBody = true)]
    public static bool IsInFutureUtc(this DateTimeOffset value) => value > DateTimeOffset.UtcNow;
    [GenerateAssertion(ExpectationMessage = "to be in the past (UTC)", InlineMethodBody = true)]
    public static bool IsInPastUtc(this DateTimeOffset value) => value < DateTimeOffset.UtcNow;
    [GenerateAssertion(ExpectationMessage = "to be on a weekend", InlineMethodBody = true)]
    public static bool IsOnWeekend(this DateTimeOffset value) => value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek == DayOfWeek.Sunday;
    [GenerateAssertion(ExpectationMessage = "to be on a weekday", InlineMethodBody = true)]
    public static bool IsOnWeekday(this DateTimeOffset value) => value.DayOfWeek != DayOfWeek.Saturday && value.DayOfWeek != DayOfWeek.Sunday;
}
