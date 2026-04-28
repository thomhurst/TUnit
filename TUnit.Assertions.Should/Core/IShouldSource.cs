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
}
