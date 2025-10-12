using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for TimeSpan type using [GenerateAssertion] attributes.
/// </summary>
public static class TimeSpanAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this TimeSpan value) => value == TimeSpan.Zero;

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this TimeSpan value) => value != TimeSpan.Zero;

    [GenerateAssertion(ExpectationMessage = "to be positive")]
    public static bool IsPositive(this TimeSpan value) => value > TimeSpan.Zero;

    [GenerateAssertion(ExpectationMessage = "to be negative")]
    public static bool IsNegative(this TimeSpan value) => value < TimeSpan.Zero;

    [GenerateAssertion(ExpectationMessage = "to be non-negative")]
    public static bool IsNonNegative(this TimeSpan value) => value >= TimeSpan.Zero;

    [GenerateAssertion(ExpectationMessage = "to be non-positive")]
    public static bool IsNonPositive(this TimeSpan value) => value <= TimeSpan.Zero;
}
