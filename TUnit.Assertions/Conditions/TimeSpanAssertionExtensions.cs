using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for TimeSpan type using [GenerateAssertion(InlineMethodBody = true)] attributes.
/// </summary>
file static partial class TimeSpanAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this TimeSpan value) => value == TimeSpan.Zero;
    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this TimeSpan value) => value != TimeSpan.Zero;
    [GenerateAssertion(ExpectationMessage = "to be positive", InlineMethodBody = true)]
    public static bool IsPositive(this TimeSpan value) => value > TimeSpan.Zero;
    [GenerateAssertion(ExpectationMessage = "to be negative", InlineMethodBody = true)]
    public static bool IsNegative(this TimeSpan value) => value < TimeSpan.Zero;
    [GenerateAssertion(ExpectationMessage = "to be non-negative", InlineMethodBody = true)]
    public static bool IsNonNegative(this TimeSpan value) => value >= TimeSpan.Zero;
    [GenerateAssertion(ExpectationMessage = "to be non-positive", InlineMethodBody = true)]
    public static bool IsNonPositive(this TimeSpan value) => value <= TimeSpan.Zero;
}
