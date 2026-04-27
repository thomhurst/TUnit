#if !NETSTANDARD2_0
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Specialised overloads for LastItem(...).Satisfies(...) when the last item is itself
/// a collection-like value. Each overload delegates to the generic
/// <c>Satisfies&lt;TSource&gt;</c> on the last-item source, picking the matching
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
/// <see cref="ListLastItemSource{TList,TItem}"/> and
/// <see cref="ReadOnlyListLastItemSource{TList,TItem}"/>; otherwise users
/// must spell out the type argument explicitly: <c>.Satisfies&lt;MyAssertion&lt;T&gt;&gt;(...)</c>.
/// </remarks>
public static class ListLastItemSatisfiesExtensions
{
    public static ListLastItemSatisfiesAssertion<TList, IEnumerable<TInner>> Satisfies<TList, TInner>(
        this ListLastItemSource<TList, IEnumerable<TInner>> source,
        Func<CollectionAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IEnumerable<TInner>>
        => source.Satisfies<CollectionAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, IEnumerable<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListLastItemSource<TList, IEnumerable<TInner>> source,
        Func<CollectionAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IEnumerable<TInner>>
        => source.Satisfies<CollectionAssertion<TInner>>(assertion, expression);

    public static ListLastItemSatisfiesAssertion<TList, IList<TInner>> Satisfies<TList, TInner>(
        this ListLastItemSource<TList, IList<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IList<TInner>>
        => source.Satisfies<ListAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, IList<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListLastItemSource<TList, IList<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IList<TInner>>
        => source.Satisfies<ListAssertion<TInner>>(assertion, expression);

    public static ListLastItemSatisfiesAssertion<TList, IReadOnlyList<TInner>> Satisfies<TList, TInner>(
        this ListLastItemSource<TList, IReadOnlyList<TInner>> source,
        Func<ReadOnlyListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IReadOnlyList<TInner>>
        => source.Satisfies<ReadOnlyListAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, IReadOnlyList<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListLastItemSource<TList, IReadOnlyList<TInner>> source,
        Func<ReadOnlyListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IReadOnlyList<TInner>>
        => source.Satisfies<ReadOnlyListAssertion<TInner>>(assertion, expression);

    public static ListLastItemSatisfiesAssertion<TList, IReadOnlyDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ListLastItemSource<TList, IReadOnlyDictionary<TKey, TValue>> source,
        Func<DictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<DictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, IReadOnlyDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ReadOnlyListLastItemSource<TList, IReadOnlyDictionary<TKey, TValue>> source,
        Func<DictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<DictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static ListLastItemSatisfiesAssertion<TList, IDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ListLastItemSource<TList, IDictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IDictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<MutableDictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, IDictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ReadOnlyListLastItemSource<TList, IDictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IDictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<MutableDictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static ListLastItemSatisfiesAssertion<TList, ISet<TInner>> Satisfies<TList, TInner>(
        this ListLastItemSource<TList, ISet<TInner>> source,
        Func<SetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<ISet<TInner>>
        => source.Satisfies<SetAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, ISet<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListLastItemSource<TList, ISet<TInner>> source,
        Func<SetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<ISet<TInner>>
        => source.Satisfies<SetAssertion<TInner>>(assertion, expression);

    public static ListLastItemSatisfiesAssertion<TList, IReadOnlySet<TInner>> Satisfies<TList, TInner>(
        this ListLastItemSource<TList, IReadOnlySet<TInner>> source,
        Func<ReadOnlySetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<IReadOnlySet<TInner>>
        => source.Satisfies<ReadOnlySetAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, IReadOnlySet<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListLastItemSource<TList, IReadOnlySet<TInner>> source,
        Func<ReadOnlySetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<IReadOnlySet<TInner>>
        => source.Satisfies<ReadOnlySetAssertion<TInner>>(assertion, expression);

    public static ListLastItemSatisfiesAssertion<TList, TInner[]> Satisfies<TList, TInner>(
        this ListLastItemSource<TList, TInner[]> source,
        Func<ArrayAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<TInner[]>
        => source.Satisfies<ArrayAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, TInner[]> Satisfies<TList, TInner>(
        this ReadOnlyListLastItemSource<TList, TInner[]> source,
        Func<ArrayAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<TInner[]>
        => source.Satisfies<ArrayAssertion<TInner>>(assertion, expression);

    public static ListLastItemSatisfiesAssertion<TList, List<TInner>> Satisfies<TList, TInner>(
        this ListLastItemSource<TList, List<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<List<TInner>>
        => source.Satisfies<ListAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, List<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListLastItemSource<TList, List<TInner>> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<List<TInner>>
        => source.Satisfies<ListAssertion<TInner>>(assertion, expression);

    public static ListLastItemSatisfiesAssertion<TList, HashSet<TInner>> Satisfies<TList, TInner>(
        this ListLastItemSource<TList, HashSet<TInner>> source,
        Func<HashSetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<HashSet<TInner>>
        => source.Satisfies<HashSetAssertion<TInner>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, HashSet<TInner>> Satisfies<TList, TInner>(
        this ReadOnlyListLastItemSource<TList, HashSet<TInner>> source,
        Func<HashSetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<HashSet<TInner>>
        => source.Satisfies<HashSetAssertion<TInner>>(assertion, expression);

    public static ListLastItemSatisfiesAssertion<TList, Dictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ListLastItemSource<TList, Dictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IList<Dictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<MutableDictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static ReadOnlyListLastItemSatisfiesAssertion<TList, Dictionary<TKey, TValue>> Satisfies<TList, TKey, TValue>(
        this ReadOnlyListLastItemSource<TList, Dictionary<TKey, TValue>> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TList : IReadOnlyList<Dictionary<TKey, TValue>>
        where TKey : notnull
        => source.Satisfies<MutableDictionaryAssertion<TKey, TValue>>(assertion, expression);
}
#endif
