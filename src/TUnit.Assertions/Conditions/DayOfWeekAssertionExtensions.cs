using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DayOfWeek type using [GenerateAssertion(InlineMethodBody = true)] attributes.
/// These wrap day-of-week checks as extension methods.
/// </summary>
file static partial class DayOfWeekAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be a weekend day", InlineMethodBody = true)]
    public static bool IsWeekend(this DayOfWeek value) =>
        value == DayOfWeek.Saturday || value == DayOfWeek.Sunday;
    [GenerateAssertion(ExpectationMessage = "to be a weekday", InlineMethodBody = true)]
    public static bool IsWeekday(this DayOfWeek value) =>
        value != DayOfWeek.Saturday && value != DayOfWeek.Sunday;
    [GenerateAssertion(ExpectationMessage = "to be Monday", InlineMethodBody = true)]
    public static bool IsMonday(this DayOfWeek value) => value == DayOfWeek.Monday;
    [GenerateAssertion(ExpectationMessage = "to be Friday", InlineMethodBody = true)]
    public static bool IsFriday(this DayOfWeek value) => value == DayOfWeek.Friday;
}
