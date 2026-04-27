#if !NETSTANDARD2_0
using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Specialised <c>Satisfies</c> overloads when the selected item (from <c>ItemAt</c>,
/// <c>LastItem</c>, etc.) is itself a collection-like value. Each overload binds the
/// matching <see cref="IAssertionSourceFor{TItem,TSelf}"/> implementation and dispatches
/// through <see cref="IItemSatisfiesSource{TItem,TResult}.Satisfies{TSource}"/>,
/// so a single set of per-shape overloads serves every item-selector source type.
/// </summary>
/// <remarks>
/// Both interface-shaped (e.g. <c>IList&lt;T&gt;</c>) and concrete-shaped
/// (e.g. <c>List&lt;T&gt;</c>) overloads are required because C# overload
/// resolution performs exact type matching on the source's <c>TItem</c>:
/// <c>IList&lt;List&lt;int&gt;&gt;</c> binds the <c>List&lt;TInner&gt;</c>
/// overload, never the <c>IList&lt;TInner&gt;</c> one.
///
/// When introducing a new <see cref="IAssertionSourceFor{TItem,TSelf}"/>
/// implementation, add the matching overload below; otherwise users must
/// spell out the type argument explicitly: <c>.Satisfies&lt;MyAssertion&lt;T&gt;&gt;(...)</c>.
/// </remarks>
public static class CollectionItemSatisfiesExtensions
{
    public static TResult Satisfies<TInner, TResult>(
        this IItemSatisfiesSource<IEnumerable<TInner>, TResult> source,
        Func<CollectionAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        => source.Satisfies<CollectionAssertion<TInner>>(assertion, expression);

    public static TResult Satisfies<TInner, TResult>(
        this IItemSatisfiesSource<IList<TInner>, TResult> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        => source.Satisfies<ListAssertion<TInner>>(assertion, expression);

    public static TResult Satisfies<TInner, TResult>(
        this IItemSatisfiesSource<IReadOnlyList<TInner>, TResult> source,
        Func<ReadOnlyListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        => source.Satisfies<ReadOnlyListAssertion<TInner>>(assertion, expression);

    public static TResult Satisfies<TKey, TValue, TResult>(
        this IItemSatisfiesSource<IReadOnlyDictionary<TKey, TValue>, TResult> source,
        Func<DictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TKey : notnull
        => source.Satisfies<DictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static TResult Satisfies<TKey, TValue, TResult>(
        this IItemSatisfiesSource<IDictionary<TKey, TValue>, TResult> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TKey : notnull
        => source.Satisfies<MutableDictionaryAssertion<TKey, TValue>>(assertion, expression);

    public static TResult Satisfies<TInner, TResult>(
        this IItemSatisfiesSource<ISet<TInner>, TResult> source,
        Func<SetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        => source.Satisfies<SetAssertion<TInner>>(assertion, expression);

    public static TResult Satisfies<TInner, TResult>(
        this IItemSatisfiesSource<IReadOnlySet<TInner>, TResult> source,
        Func<ReadOnlySetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        => source.Satisfies<ReadOnlySetAssertion<TInner>>(assertion, expression);

    public static TResult Satisfies<TInner, TResult>(
        this IItemSatisfiesSource<TInner[], TResult> source,
        Func<ArrayAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        => source.Satisfies<ArrayAssertion<TInner>>(assertion, expression);

    public static TResult Satisfies<TInner, TResult>(
        this IItemSatisfiesSource<List<TInner>, TResult> source,
        Func<ListAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        => source.Satisfies<ListAssertion<TInner>>(assertion, expression);

    public static TResult Satisfies<TInner, TResult>(
        this IItemSatisfiesSource<HashSet<TInner>, TResult> source,
        Func<HashSetAssertion<TInner>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        => source.Satisfies<HashSetAssertion<TInner>>(assertion, expression);

    public static TResult Satisfies<TKey, TValue, TResult>(
        this IItemSatisfiesSource<Dictionary<TKey, TValue>, TResult> source,
        Func<MutableDictionaryAssertion<TKey, TValue>, IAssertion?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
        where TKey : notnull
        => source.Satisfies<MutableDictionaryAssertion<TKey, TValue>>(assertion, expression);
}
#endif
