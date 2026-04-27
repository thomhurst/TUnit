#if !NETSTANDARD2_0
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Specialised overloads for ItemAt(...).Satisfies(...) when the item is itself
/// a collection-like value. Each overload delegates to the generic
/// <c>Satisfies&lt;TSource&gt;</c> on the item-at source, picking the matching
/// <see cref="IAssertionSourceFor{TItem,TSelf}"/> implementation.
/// </summary>
/// <remarks>
/// Both interface-shaped (e.g. <c>IList&lt;T&gt;</c>) and concrete-shaped
/// (e.g. <c>List&lt;T&gt;</c>) overloads are required because C# overload
/// resolution performs exact type matching on the source's <c>TItem</c>:
/// <c>IList&lt;List&lt;int&gt;&gt;</c> binds the <c>List&lt;TInner&gt;</c>
/// overload, never the <c>IList&lt;TInner&gt;</c> one.
///
/// When introducing a new <see cref="IAssertionSourceFor{TItem,TSelf}"/>
/// implementation, add the matching overload pair below for both
/// <see cref="ListItemAtSource{TList,TItem}"/> and
/// <see cref="ReadOnlyListItemAtSource{TList,TItem}"/>; otherwise users
/// must spell out the type argument explicitly: <c>.Satisfies&lt;MyAssertion&lt;T&gt;&gt;(...)</c>.
///
/// Changes here must also be mirrored in <c>ListLastItemSatisfiesExtensions</c>
/// (and vice versa) — both files enumerate the same collection shapes but bind
/// to different item-selector sources.
/// </remarks>
public static class ListItemAtSatisfiesExtensions
{
    public static ListItemAtSatisfiesAssertion<TList, IEnumerable<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, IEnumerable<TInner>> source,
        Func<CollectionAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IEnumerable<TInner>>
        => source.Satisfies<CollectionAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IEnumerable<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, IEnumerable<TInner>> source,
        Func<CollectionAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IEnumerable<TInner>>
        => source.Satisfies<CollectionAssertion<TInner>>(assertion, expression);

    public static ListItemAtSatisfiesAssertion<TList, IList<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, IList<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IList<TInner>>
        => source.Satisfies<ListAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IList<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, IList<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IList<TInner>>
        => source.Satisfies<ListAssertion<TInner>>(assertion, expression);

    public static ListItemAtSatisfiesAssertion<TList, IReadOnlyList<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, IReadOnlyList<TInner>> source,
        Func<ReadOnlyListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IReadOnlyList<TInner>>
        => source.Satisfies<ReadOnlyListAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IReadOnlyList<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, IReadOnlyList<TInner>> source,
        Func<ReadOnlyListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IReadOnlyList<TInner>>
        => source.Satisfies<ReadOnlyListAssertion<TInner>>(assertion, expression);

    public static ListItemAtSatisfiesAssertion<TList, IReadOnlyDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ListItemAtSource<TList, IReadOnlyDictionary<TKey, TValue>> source,
        Func<DictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<DictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IReadOnlyDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ReadOnlyListItemAtSource<TList, IReadOnlyDictionary<TKey, TValue>> source,
        Func<DictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<DictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static ListItemAtSatisfiesAssertion<TList, IDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ListItemAtSource<TList, IDictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IDictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<MutableDictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ReadOnlyListItemAtSource<TList, IDictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IDictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<MutableDictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static ListItemAtSatisfiesAssertion<TList, ISet<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, ISet<TInner>> source,
        Func<SetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<ISet<TInner>>
        => source.Satisfies<SetAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, ISet<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, ISet<TInner>> source,
        Func<SetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<ISet<TInner>>
        => source.Satisfies<SetAssertion<TInner>>(assertion, expression);

    public static ListItemAtSatisfiesAssertion<TList, IReadOnlySet<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, IReadOnlySet<TInner>> source,
        Func<ReadOnlySetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IReadOnlySet<TInner>>
        => source.Satisfies<ReadOnlySetAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, IReadOnlySet<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, IReadOnlySet<TInner>> source,
        Func<ReadOnlySetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IReadOnlySet<TInner>>
        => source.Satisfies<ReadOnlySetAssertion<TInner>>(assertion, expression);

    public static ListItemAtSatisfiesAssertion<TList, TInner[]> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, TInner[]> source,
        Func<ArrayAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<TInner[]>
        => source.Satisfies<ArrayAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, TInner[]> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, TInner[]> source,
        Func<ArrayAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<TInner[]>
        => source.Satisfies<ArrayAssertion<TInner>>(assertion, expression);

    public static ListItemAtSatisfiesAssertion<TList, List<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, List<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<List<TInner>>
        => source.Satisfies<ListAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, List<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, List<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<List<TInner>>
        => source.Satisfies<ListAssertion<TInner>>(assertion, expression);

    public static ListItemAtSatisfiesAssertion<TList, HashSet<TInner>> Satisfies<TList, TInner>(
        this ListItemAtSource<TList, HashSet<TInner>> source,
        Func<HashSetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<HashSet<TInner>>
        => source.Satisfies<HashSetAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, HashSet<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListItemAtSource<TList, HashSet<TInner>> source,
        Func<HashSetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<HashSet<TInner>>
        => source.Satisfies<HashSetAssertion<TInner>>(assertion, expression);

    public static ListItemAtSatisfiesAssertion<TList, Dictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ListItemAtSource<TList, Dictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<Dictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<MutableDictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static ReadOnlyListItemAtSatisfiesAssertion<TList, Dictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ReadOnlyListItemAtSource<TList, Dictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<Dictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<MutableDictionaryAssertion<TKey, TValue>>(assertion, expression);
}
#endif
