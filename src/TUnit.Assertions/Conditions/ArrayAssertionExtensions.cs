using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Array and Collection types using [GenerateAssertion(InlineMethodBody = true)] attributes.
/// These wrap array and collection checks as extension methods.
/// </summary>
file static partial class ArrayAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be an empty array", InlineMethodBody = true)]
    public static bool IsEmpty<T>(this T[] value) => value?.Length == 0;
    [GenerateAssertion(ExpectationMessage = "to not be an empty array", InlineMethodBody = true)]
    public static bool IsNotEmpty<T>(this T[] value) => value != null && value.Length > 0;
    [GenerateAssertion(ExpectationMessage = "to be a single-element array", InlineMethodBody = true)]
    public static bool IsSingleElement<T>(this T[] value) => value?.Length == 1;
    [GenerateAssertion(ExpectationMessage = "to not be a single-element array", InlineMethodBody = true)]
    public static bool IsNotSingleElement<T>(this T[] value) => value?.Length != 1;
    [GenerateAssertion(ExpectationMessage = "to be a single-element collection", InlineMethodBody = true)]
    public static bool IsSingleElement<T>(this IEnumerable<T> value) => value != null && value.Skip(1).Take(1).Any() == false && value.Any();
    [GenerateAssertion(ExpectationMessage = "to not be a single-element collection", InlineMethodBody = true)]
    public static bool IsNotSingleElement<T>(this IEnumerable<T> value) => value == null || value.Skip(1).Take(1).Any() || !value.Any();
}
