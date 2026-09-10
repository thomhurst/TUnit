#if !NETSTANDARD2_0
namespace TUnit.Assertions.Core;

/// <summary>
/// Marks an assertion source as constructible from a raw value, enabling generic
/// dispatch through <c>Satisfies&lt;TSource&gt;</c> on item-at and similar entry points.
/// Implementations expose a static factory used to materialise the specialised source
/// per-item without per-shape overload enumeration.
/// </summary>
/// <remarks>
/// Intentionally does not extend <see cref="IAssertionSource{TItem}"/>: a single
/// implementing class can implement multiple parameterisations of this interface
/// (e.g. against both an interface item type and a matching concrete type) without
/// triggering conflicting <c>Context</c> property requirements.
/// </remarks>
/// <typeparam name="TItem">The value type the source wraps.</typeparam>
/// <typeparam name="TSelf">The implementing source type (CRTP).</typeparam>
public interface IAssertionSourceFor<TItem, TSelf>
    where TSelf : IAssertionSourceFor<TItem, TSelf>
{
    /// <summary>
    /// Constructs the specialised assertion source wrapping <paramref name="item"/> with the
    /// given <paramref name="label"/>.
    /// </summary>
    /// <remarks>
    /// Implementations may receive a <see langword="null"/> <paramref name="item"/> when the
    /// caller selects from a collection that allows nulls. Whether the resulting assertion
    /// surfaces a graceful failure or throws on a subsequent operation is the implementer's
    /// responsibility — the contract is "construct a source that callers can run assertions
    /// against", not "validate non-null up front".
    /// </remarks>
    static abstract TSelf Create(TItem item, string label);
}
#endif
