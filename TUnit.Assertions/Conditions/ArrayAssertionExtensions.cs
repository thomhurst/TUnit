using System.ComponentModel;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Array and Collection types using [GenerateAssertion] attributes.
/// These wrap array and collection checks as extension methods.
/// </summary>
public static partial class ArrayAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be an empty array")]
    public static bool IsEmpty<T>(this T[] value) => value?.Length == 0;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be an empty array")]
    public static bool IsNotEmpty<T>(this T[] value) => value != null && value.Length > 0;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a single-element array")]
    public static bool IsSingleElement<T>(this T[] value) => value?.Length == 1;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be a single-element array")]
    public static bool IsNotSingleElement<T>(this T[] value) => value?.Length != 1;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a single-element collection")]
    public static bool IsSingleElement<T>(this IEnumerable<T> value)
    {
        if (value == null) return false;
        using var enumerator = value.GetEnumerator();
        if (!enumerator.MoveNext()) return false; // Empty
        return !enumerator.MoveNext(); // True if no second element
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be a single-element collection")]
    public static bool IsNotSingleElement<T>(this IEnumerable<T> value)
    {
        if (value == null) return true;
        using var enumerator = value.GetEnumerator();
        if (!enumerator.MoveNext()) return true; // Empty - not single element
        return enumerator.MoveNext(); // True if there's a second element
    }
}
