using System.ComponentModel;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

/// <summary>
/// Non-generic marker for all Should-flavored assertion sources.
/// </summary>
public interface IShouldSource;

/// <summary>
/// Marker interface for Should-flavored assertion sources. Generated extension
/// methods target this type and read the assertion context via <see cref="Context"/>.
/// Element-type inference for collection-style assertions is achieved through
/// method-level generic constraints (<c>where TActual : IEnumerable&lt;TItem&gt;</c>),
/// not interface variance.
/// </summary>
/// <typeparam name="T">The type of value being asserted.</typeparam>
public interface IShouldSource<T> : IShouldSource
{
    AssertionContext<T> Context { get; }

    /// <summary>
    /// Consumes a pending <c>Because</c> message set before the next assertion is created.
    /// Used by generated extensions to apply pre-chain reasons to the assertion instance.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    string? ConsumeBecauseMessage();
}
