using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Assertions;

file static class GenericAssertions
{
    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsIn<T>(this T value, IEnumerable<T> collection) => collection.Contains(value);

    // TODO: This overload cannot be inlined due to nullability issues with IEqualityComparer<T>
    // and cannot be called as a non-inlined method due to generator limitations
    // [GenerateAssertion]
    // public static bool IsIn<T>(this T value, IEnumerable<T> collection, IEqualityComparer<T>? equalityComparer) => collection.Contains(value, equalityComparer);

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsIn<T>(this T value, params T[] collection) => collection.Contains(value);

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsNotIn<T>(this T value, IEnumerable<T> collection) => !collection.Contains(value);

    // TODO: This overload cannot be inlined due to nullability issues with IEqualityComparer<T>
    // and cannot be called as a non-inlined method due to generator limitations
    // [GenerateAssertion]
    // public static bool IsNotIn<T>(this T value, IEnumerable<T> collection, IEqualityComparer<T>? equalityComparer) => !collection.Contains(value, equalityComparer);

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsNotIn<T>(this T value, params T[] collection) => !collection.Contains(value);
}
