using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DayOfWeek type using [GenerateAssertion] attributes.
/// These wrap day-of-week checks as extension methods.
/// </summary>
public static class DayOfWeekAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be a weekend day")]
    public static bool IsWeekend(this DayOfWeek value) =>
        value == DayOfWeek.Saturday || value == DayOfWeek.Sunday;

    [GenerateAssertion(ExpectationMessage = "to be a weekday")]
    public static bool IsWeekday(this DayOfWeek value) =>
        value != DayOfWeek.Saturday && value != DayOfWeek.Sunday;

    [GenerateAssertion(ExpectationMessage = "to be Monday")]
    public static bool IsMonday(this DayOfWeek value) => value == DayOfWeek.Monday;

    [GenerateAssertion(ExpectationMessage = "to be Friday")]
    public static bool IsFriday(this DayOfWeek value) => value == DayOfWeek.Friday;
}
