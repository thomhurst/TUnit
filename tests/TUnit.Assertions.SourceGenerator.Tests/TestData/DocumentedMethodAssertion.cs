using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: XML documentation on [GenerateAssertion] methods. Users only see the generated
/// extension methods, so a documented public source method must pass its documentation on to them,
/// whatever its containing type. A package reference does not expose non-public methods, so those
/// keep the placeholder.
/// </summary>
public static partial class DocumentedMethodAssertions
{
    /// <summary>
    /// Checks that the value is a multiple of <paramref name="factor"/>.
    /// </summary>
    /// <param name="value">The value under test.</param>
    /// <param name="factor">The factor; must not be zero.</param>
    /// <returns><see langword="true"/> if the value is a multiple of <paramref name="factor"/>.</returns>
    [GenerateAssertion(ExpectationMessage = "to be a multiple of {factor}")]
    public static bool IsMultipleOf(this int value, int factor) => value % factor == 0;

    /// <summary>
    /// Checks that the value is a multiple of <paramref name="factor"/>.
    /// </summary>
    /// <param name="factor">The factor; must not be zero.</param>
    [GenerateAssertion(ExpectationMessage = "to be a multiple of {factor}")]
    public static bool IsMultipleOf(this long value, long factor) => value % factor == 0;

    /// <summary>
    /// Checks that the value equals <paramref name="other"/> by <see cref="object.Equals(object, object)"/>.
    /// </summary>
    /// <typeparam name="T">The type of the compared values.</typeparam>
    /// <param name="other">The value to compare with.</param>
    [GenerateAssertion(ExpectationMessage = "to equal {other}")]
    public static bool IsSameValueAs<T>(this T value, T other) => Equals(value, other);

    [GenerateAssertion(ExpectationMessage = "to be odd")]
    public static bool IsOdd(this int value) => value % 2 != 0;

    /// <summary>
    /// Checks that the value is a power of two.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be a power of two", InlineMethodBody = true)]
    internal static bool IsPowerOfTwo(this int value) => value > 0 && (value & (value - 1)) == 0;

    private static class NestedAssertions
    {
        /// <summary>
        /// Checks that the value is divisible by three.
        /// </summary>
        [GenerateAssertion(ExpectationMessage = "to be divisible by three", InlineMethodBody = true)]
        public static bool IsDivisibleByThree(int value) => value % 3 == 0;
    }
}

internal static class InternalDocumentedAssertions
{
    /// <summary>
    /// Checks that the text has no lower-case letters.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be upper case", InlineMethodBody = true)]
    public static bool IsUpperCase(this string value) => value.ToUpperInvariant() == value;
}

file static class FileLocalDocumentedAssertions
{
    /// <summary>
    /// Checks that the text has no leading or trailing white space.
    /// </summary>
    /// <param name="text">The text under test.</param>
    [GenerateAssertion(ExpectationMessage = "to have no leading or trailing white space", InlineMethodBody = true)]
    public static bool IsTrimmed(this string text) => text.Trim() == text;
}
