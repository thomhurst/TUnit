namespace TUnit.Core.Extensions;

public static class EnumerableExtensions
{
    public static UniqueElementsEnumerable<T> ToUniqueElementsEnumerable<T>(this IEnumerable<T> enumerable)
    {
        return new UniqueElementsEnumerable<T>(enumerable);
    }
}