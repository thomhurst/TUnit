using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Assertions;

/// <summary>
/// Strict equality assertions that use <see cref="object.Equals(object, object)"/>
/// instead of <see cref="EqualityComparer{T}.Default"/>.
/// This is useful when you want to compare without using IEquatable&lt;T&gt; implementations.
/// </summary>
file static class StrictEqualityAssertions
{
    /// <summary>
    /// Asserts that the value is strictly equal to the expected value using <see cref="object.Equals(object, object)"/>.
    /// Unlike <c>IsEqualTo</c>, this does not use <see cref="IEquatable{T}"/> or custom equality comparers.
    /// </summary>
    [GenerateAssertion(InlineMethodBody = true, ExpectationMessage = "be strictly equal to {expected}")]
    public static bool IsStrictlyEqualTo<T>(this T value, T expected) => object.Equals(value, expected);

    /// <summary>
    /// Asserts that the value is not strictly equal to the expected value using <see cref="object.Equals(object, object)"/>.
    /// Unlike <c>IsNotEqualTo</c>, this does not use <see cref="IEquatable{T}"/> or custom equality comparers.
    /// </summary>
    [GenerateAssertion(InlineMethodBody = true, ExpectationMessage = "not be strictly equal to {expected}")]
    public static bool IsNotStrictlyEqualTo<T>(this T value, T expected) => !object.Equals(value, expected);
}
