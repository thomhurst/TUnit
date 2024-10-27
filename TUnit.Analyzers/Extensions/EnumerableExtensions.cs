namespace TUnit.Analyzers.Extensions;

public static class EnumerableExtensions
{
    public static T? FirstOrNull<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) where T : struct
    {
        if (enumerable is null)
        {
            throw new ArgumentNullException(nameof(enumerable));
        }

        var filtered = enumerable.Where(predicate).ToArray();

        if (!filtered.Any())
        {
            return null;
        }

        return filtered[0];
    }
}