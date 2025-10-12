using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DateTime type using [GenerateAssertion] attributes.
/// These wrap DateTime property and method checks as extension methods.
/// </summary>
public static class DateTimeAssertionExtensions
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
}
