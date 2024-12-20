using System.Diagnostics.CodeAnalysis;

namespace TUnit;

static class PolyfillExtensions
{
    internal static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V val)
    {
        key = pair.Key;
        val = pair.Value;
    }

    internal static string ReplaceLineEndings(this string value, string replacement)
    {
        return value.Replace("\r\n", replacement)
            .Replace("\n", replacement)
            .Replace("\r", replacement);
    }

    internal static bool StartsWith(this string value, char ch) => value.Length > 0 && value[0] == ch;

    internal static bool TryPop<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T value)
    {
        if (stack.Count == 0)
        {
            value = default;
            return false;
        }

        value = stack.Pop();
        return true;
    }

    internal static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source) => source.OrderBy(v => v);

    /// <summary>Returns the minimum value in a generic sequence according to a specified key selector function.</summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>The value with the minimum key in the sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="System.IComparable{TKey}" /> interface.</exception>
    /// <remarks>
    /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
    /// </remarks>
    internal static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => MinBy(source, keySelector, comparer: null);

    /// <summary>Returns the minimum value in a generic sequence according to a specified key selector function.</summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="comparer">The <see cref="IComparer{TKey}" /> to compare keys.</param>
    /// <returns>The value with the minimum key in the sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{TKey}" /> interface.</exception>
    /// <remarks>
    /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
    /// </remarks>
    internal static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
    {
        Verify.ArgNotNull(source);
        Verify.ArgNotNull(keySelector);

        comparer ??= Comparer<TKey>.Default;

        using IEnumerator<TSource> e = source.GetEnumerator();

        if (!e.MoveNext())
        {
            if (default(TSource) is null)
            {
                return default;
            }
            else
            {
                throw new InvalidOperationException("No elements.");
            }
        }

        TSource value = e.Current;
        TKey key = keySelector(value);

        if (default(TKey) is null)
        {
            if (key is null)
            {
                TSource firstValue = value;

                do
                {
                    if (!e.MoveNext())
                    {
                        // All keys are null, surface the first element.
                        return firstValue;
                    }

                    value = e.Current;
                    key = keySelector(value);
                }
                while (key is null);
            }

            while (e.MoveNext())
            {
                TSource nextValue = e.Current;
                TKey nextKey = keySelector(nextValue);
                if (nextKey is not null && comparer.Compare(nextKey, key) < 0)
                {
                    key = nextKey;
                    value = nextValue;
                }
            }
        }
        else
        {
            if (comparer == Comparer<TKey>.Default)
            {
                while (e.MoveNext())
                {
                    TSource nextValue = e.Current;
                    TKey nextKey = keySelector(nextValue);
                    if (Comparer<TKey>.Default.Compare(nextKey, key) < 0)
                    {
                        key = nextKey;
                        value = nextValue;
                    }
                }
            }
            else
            {
                while (e.MoveNext())
                {
                    TSource nextValue = e.Current;
                    TKey nextKey = keySelector(nextValue);
                    if (comparer.Compare(nextKey, key) < 0)
                    {
                        key = nextKey;
                        value = nextValue;
                    }
                }
            }
        }

        return value;
    }

    /// <summary>Returns the maximum value in a generic sequence according to a specified key selector function.</summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>The value with the maximum key in the sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="System.IComparable{TKey}" /> interface.</exception>
    /// <remarks>
    /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
    /// </remarks>
    internal static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => MaxBy(source, keySelector, null);

    /// <summary>Returns the maximum value in a generic sequence according to a specified key selector function.</summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="comparer">The <see cref="IComparer{TKey}" /> to compare keys.</param>
    /// <returns>The value with the maximum key in the sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{TKey}" /> interface.</exception>
    /// <remarks>
    /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
    /// </remarks>
    internal static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
    {
        Verify.ArgNotNull(source);
        Verify.ArgNotNull(keySelector);

        comparer ??= Comparer<TKey>.Default;

        using IEnumerator<TSource> e = source.GetEnumerator();

        if (!e.MoveNext())
        {
            if (default(TSource) is null)
            {
                return default;
            }
            else
            {
                throw new InvalidOperationException("No elements.");
            }
        }

        TSource value = e.Current;
        TKey key = keySelector(value);

        if (default(TKey) is null)
        {
            if (key is null)
            {
                TSource firstValue = value;

                do
                {
                    if (!e.MoveNext())
                    {
                        // All keys are null, surface the first element.
                        return firstValue;
                    }

                    value = e.Current;
                    key = keySelector(value);
                }
                while (key is null);
            }

            while (e.MoveNext())
            {
                TSource nextValue = e.Current;
                TKey nextKey = keySelector(nextValue);
                if (nextKey is not null && comparer.Compare(nextKey, key) > 0)
                {
                    key = nextKey;
                    value = nextValue;
                }
            }
        }
        else
        {
            if (comparer == Comparer<TKey>.Default)
            {
                while (e.MoveNext())
                {
                    TSource nextValue = e.Current;
                    TKey nextKey = keySelector(nextValue);
                    if (Comparer<TKey>.Default.Compare(nextKey, key) > 0)
                    {
                        key = nextKey;
                        value = nextValue;
                    }
                }
            }
            else
            {
                while (e.MoveNext())
                {
                    TSource nextValue = e.Current;
                    TKey nextKey = keySelector(nextValue);
                    if (comparer.Compare(nextKey, key) > 0)
                    {
                        key = nextKey;
                        value = nextValue;
                    }
                }
            }
        }

        return value;
    }
}
