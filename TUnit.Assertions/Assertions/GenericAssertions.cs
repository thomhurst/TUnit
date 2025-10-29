using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Assertions;

internal static class GenericAssertions
{
    [GenerateAssertion]
    public static bool IsIn<T>(this T value, IEnumerable<T> collection)
    {
        return collection.Contains(value);
    }

    [GenerateAssertion]
    public static bool IsIn<T>(this T value, IEnumerable<T> collection, IEqualityComparer<T> equalityComparer)
    {
        return collection.Contains(value, equalityComparer);
    }

    [GenerateAssertion]
    public static bool IsIn<T>(this T value, params T[] collection)
    {
        return collection.Contains(value);
    }

    [GenerateAssertion]
    public static bool IsNotIn<T>(this T value, IEnumerable<T> collection)
    {
        return !collection.Contains(value);
    }

    [GenerateAssertion]
    public static bool IsNotIn<T>(this T value, IEnumerable<T> collection, IEqualityComparer<T> equalityComparer)
    {
        return !collection.Contains(value, equalityComparer);
    }

    [GenerateAssertion]
    public static bool IsNotIn<T>(this T value, params T[] collection)
    {
        return !collection.Contains(value);
    }
}
