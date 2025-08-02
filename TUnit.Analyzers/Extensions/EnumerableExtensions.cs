namespace TUnit.Analyzers.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> ZipAll<T1, T2, T>(
        this IEnumerable<T1> first,
        IEnumerable<T2> second,
        Func<T1?, T2?, T> operation)
    {
        using var iter1 = first.GetEnumerator();
        using var iter2 = second.GetEnumerator();

        while (iter1.MoveNext())
        {
            if (iter2.MoveNext())
            {
                yield return operation(iter1.Current, iter2.Current);
            }
            else
            {
                yield return operation(iter1.Current, default(T2?));
            }
        }
        while (iter2.MoveNext())
        {
            yield return operation(default(T1?), iter2.Current);
        }
    }

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
