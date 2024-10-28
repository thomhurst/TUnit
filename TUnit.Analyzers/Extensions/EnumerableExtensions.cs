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

    public static IEnumerable<T> TakeUntil<T>(
        this IEnumerable<T> elements,
        Func<T, bool> predicate
    )
    {
        return elements.Select((x, i) => new { Item = x, Index = i })
            .TakeUntil((x, i) => predicate(x.Item))
            .Select(x => x.Item);
    }

    public static IEnumerable<T> TakeUntil<T>(
        this IEnumerable<T> elements,
        Func<T, int, bool> predicate
    )
    {
        var i = 0;

        foreach (var element in elements)
        {
            if (predicate(element, i))
            {
                yield return element;
                yield break;
            }

            yield return element;
            i++;
        }
    }
}