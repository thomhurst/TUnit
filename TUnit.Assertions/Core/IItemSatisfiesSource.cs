#if !NETSTANDARD2_0
namespace TUnit.Assertions.Core;

/// <summary>
/// Common shape for item-selector sources (ItemAt, LastItem, etc.) that expose a
/// generic <c>Satisfies&lt;TSource&gt;</c> entry. Lets a single set of per-shape
/// extension overloads dispatch across all selector variants by binding to this
/// interface rather than each concrete source type.
/// </summary>
/// <remarks>
/// Extends <see cref="IAssertionSource{TItem}"/> so that extension methods targeting
/// this interface bind in preference to those targeting <see cref="IAssertionSource{TItem}"/>
/// directly — without that, the predicate-style <c>Satisfies(Func&lt;TItem, bool&gt;)</c>
/// extension can win overload resolution for items whose specialised assertion methods
/// return <c>bool</c> (e.g. <c>IDictionary.ContainsKey</c>).
/// </remarks>
/// <typeparam name="TItem">The selected item's type.</typeparam>
/// <typeparam name="TResult">The selector-specific assertion result type.</typeparam>
public interface IItemSatisfiesSource<TItem, TResult> : IAssertionSource<TItem>
{
    TResult Satisfies<TSource>(Func<TSource, IAssertion?> assertion, string? expression = null)
        where TSource : IAssertionSourceFor<TItem, TSource>;
}
#endif
