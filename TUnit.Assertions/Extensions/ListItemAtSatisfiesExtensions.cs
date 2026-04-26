using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Specialised overloads for ItemAt(...).Satisfies(...) when the item is itself
/// a collection-like value.
/// </summary>
public static class ListItemAtSatisfiesExtensions
{
    public static ListItemAtSatisfiesAssertion<TList, IEnumerable<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, IEnumerable<TInner>> source,
        Func<CollectionAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IEnumerable<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new CollectionAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IEnumerable<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, IEnumerable<TInner>> source,
        Func<CollectionAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IEnumerable<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new CollectionAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ListItemAtSatisfiesAssertion<TList, IList<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, IList<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IList<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new ListAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IList<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, IList<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IList<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new ListAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ListItemAtSatisfiesAssertion<TList, IReadOnlyList<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, IReadOnlyList<TInner>> source,
        Func<ReadOnlyListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IReadOnlyList<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new ReadOnlyListAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IReadOnlyList<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, IReadOnlyList<TInner>> source,
        Func<ReadOnlyListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IReadOnlyList<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new ReadOnlyListAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ListItemAtSatisfiesAssertion<TList, IReadOnlyDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ListItemAtSource<TList, IReadOnlyDictionary<TKey, TValue>> source,
        Func<DictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new DictionaryAssertion<TKey, TValue>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IReadOnlyDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ReadOnlyListItemAtSource<TList, IReadOnlyDictionary<TKey, TValue>> source,
        Func<DictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new DictionaryAssertion<TKey, TValue>(item, $"item[{index}]")),
            expression);
    }

    public static ListItemAtSatisfiesAssertion<TList, IDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ListItemAtSource<TList, IDictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IDictionary<TKey, TValue>>
        where TKey : notnull
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new MutableDictionaryAssertion<TKey, TValue>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ReadOnlyListItemAtSource<TList, IDictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IDictionary<TKey, TValue>>
        where TKey : notnull
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new MutableDictionaryAssertion<TKey, TValue>(item, $"item[{index}]")),
            expression);
    }

    public static ListItemAtSatisfiesAssertion<TList, ISet<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, ISet<TInner>> source,
        Func<SetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<ISet<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new SetAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, ISet<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, ISet<TInner>> source,
        Func<SetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<ISet<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new SetAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

#if NET5_0_OR_GREATER
    public static ListItemAtSatisfiesAssertion<TList, IReadOnlySet<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, IReadOnlySet<TInner>> source,
        Func<ReadOnlySetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IReadOnlySet<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new ReadOnlySetAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IReadOnlySet<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, IReadOnlySet<TInner>> source,
        Func<ReadOnlySetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IReadOnlySet<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new ReadOnlySetAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }
#endif

    public static ListItemAtSatisfiesAssertion<TList, TInner[]> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, TInner[]> source,
        Func<ArrayAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<TInner[]>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new ArrayAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, TInner[]> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, TInner[]> source,
        Func<ArrayAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<TInner[]>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new ArrayAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ListItemAtSatisfiesAssertion<TList, List<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, List<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<List<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new ListAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, List<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, List<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<List<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new ListAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ListItemAtSatisfiesAssertion<TList, HashSet<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, HashSet<TInner>> source,
        Func<HashSetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<HashSet<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new HashSetAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, HashSet<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, HashSet<TInner>> source,
        Func<HashSetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<HashSet<TInner>>
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new HashSetAssertion<TInner>(item, $"item[{index}]")),
            expression);
    }

    public static ListItemAtSatisfiesAssertion<TList, Dictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ListItemAtSource<TList, Dictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<Dictionary<TKey, TValue>>
        where TKey : notnull
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new MutableDictionaryAssertion<TKey, TValue>(item, $"item[{index}]")),
            expression);
    }

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, Dictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ReadOnlyListItemAtSource<TList, Dictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<Dictionary<TKey, TValue>>
        where TKey : notnull
    {
        return SatisfiesSpecialised(
            source,
            (item, index) => assertion(new MutableDictionaryAssertion<TKey, TValue>(item, $"item[{index}]")),
            expression);
    }

    private static ListItemAtSatisfiesAssertion<TList, TItem> SatisfiesSpecialised<TList, TItem>(
        ListItemAtSource<TList, TItem> source,
        Func<TItem, int, IAssertion?> assertionFactory,
        string? expression)
        where TList : IList<TItem>
    {
        source.InternalListContext.ExpressionBuilder.Append($".Satisfies({expression})");
        return new ListItemAtSatisfiesAssertion<TList, TItem>(
            source.InternalListContext,
            source.InternalIndex,
            assertionFactory);
    }

    private static ReadOnlyListItemAtSatisfiesAssertion<TList, TItem> SatisfiesSpecialised<TList, TItem>(
        ReadOnlyListItemAtSource<TList, TItem> source,
        Func<TItem, int, IAssertion?> assertionFactory,
        string? expression)
        where TList : IReadOnlyList<TItem>
    {
        source.InternalListContext.ExpressionBuilder.Append($".Satisfies({expression})");
        return new ReadOnlyListItemAtSatisfiesAssertion<TList, TItem>(
            source.InternalListContext,
            source.InternalIndex,
            assertionFactory);
    }
}
