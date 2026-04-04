namespace TUnit.Core;

/// <summary>
/// Implemented by data source attributes that provide trace scope information.
/// The engine uses this to parent OpenTelemetry initialization and disposal spans
/// under the correct activity (session, assembly, class, or test).
/// </summary>
/// <remarks>
/// This interface is intended as a public extension point. Third-party
/// <see cref="IDataSourceAttribute"/> implementations may implement it to
/// participate in scope-aware tracing.
/// </remarks>
public interface ITraceScopeProvider
{
    /// <summary>
    /// Returns the <see cref="SharedType"/> for each generated object.
    /// The sequence must be in the same order as the object array produced by
    /// <see cref="IDataSourceAttribute.GetDataRowsAsync"/>; position <c>i</c>
    /// in this sequence corresponds to <c>objects[i]</c> in the data row.
    /// If the sequence is shorter than the object array, remaining objects
    /// default to <see cref="SharedType.None"/> (per-test scope).
    /// </summary>
    IEnumerable<SharedType> GetSharedTypes();
}
