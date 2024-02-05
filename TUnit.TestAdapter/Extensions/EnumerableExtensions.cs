namespace TUnit.TestAdapter.Extensions;

public static class EnumerableExtensions
{
    public static Queue<T> ToQueue<T>(this IEnumerable<T> enumerable)
    {
        return new Queue<T>(enumerable);
    }
}