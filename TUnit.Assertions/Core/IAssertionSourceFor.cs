#if !NETSTANDARD2_0
namespace TUnit.Assertions.Core;

/// <summary>
/// Marks an assertion source as constructible from a raw value, enabling generic
/// dispatch through <c>Satisfies&lt;TSource&gt;</c> on item-at and similar entry points.
/// Implementations expose a static factory used to materialise the specialised source
/// per-item without per-shape overload enumeration.
/// </summary>
/// <typeparam name="TItem">The value type the source wraps.</typeparam>
/// <typeparam name="TSelf">The implementing source type (CRTP).</typeparam>
public interface IAssertionSourceFor<TItem, TSelf> : IAssertionSource<TItem>
    where TSelf : IAssertionSourceFor<TItem, TSelf>
{
    static abstract TSelf Create(TItem item, string label);
}
#endif
