#if NET6_0_OR_GREATER
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for TimeOnly type using [GenerateAssertion] attributes.
/// These wrap TimeOnly checks as extension methods.
/// </summary>
public static partial class TimeOnlyAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be midnight")]
    public static bool IsMidnight(this TimeOnly value) => value == TimeOnly.MinValue;

    [GenerateAssertion(ExpectationMessage = "to not be midnight")]
    public static bool IsNotMidnight(this TimeOnly value) => value != TimeOnly.MinValue;

    [GenerateAssertion(ExpectationMessage = "to be noon")]
    public static bool IsNoon(this TimeOnly value) => value.Hour == 12 && value.Minute == 0 && value.Second == 0 && value.Millisecond == 0;

    [GenerateAssertion(ExpectationMessage = "to be in the AM")]
    public static bool IsAM(this TimeOnly value) => value.Hour < 12;

    [GenerateAssertion(ExpectationMessage = "to be in the PM")]
    public static bool IsPM(this TimeOnly value) => value.Hour >= 12;

    [GenerateAssertion(ExpectationMessage = "to be at the start of the hour")]
    public static bool IsStartOfHour(this TimeOnly value) => value.Minute == 0 && value.Second == 0 && value.Millisecond == 0;

    [GenerateAssertion(ExpectationMessage = "to be at the end of the hour")]
    public static bool IsEndOfHour(this TimeOnly value) => value.Minute == 59 && value.Second == 59 && value.Millisecond == 999;
}
#endif
