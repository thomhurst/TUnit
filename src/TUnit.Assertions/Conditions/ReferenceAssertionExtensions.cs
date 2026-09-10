using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated reference equality assertions using [GenerateAssertion] with InlineMethodBody.
/// </summary>
file static partial class ReferenceAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be the same reference", InlineMethodBody = true)]
    public static bool IsSameReferenceAs<TValue>(this TValue value, object? expected)
        => ReferenceEquals(value, expected);

    [GenerateAssertion(ExpectationMessage = "to not be the same reference", InlineMethodBody = true)]
    public static bool IsNotSameReferenceAs<TValue>(this TValue value, object? expected)
        => !ReferenceEquals(value, expected);
}
