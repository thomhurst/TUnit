namespace TUnit.Core;

/// <summary>
/// Implemented by data source attributes that provide trace scope information.
/// The engine uses this to parent OpenTelemetry initialization and disposal spans
/// under the correct activity (session, assembly, class, or test).
/// </summary>
public interface ITraceScopeProvider
{
    /// <summary>
    /// Returns the <see cref="SharedType"/> for each generated object, in parameter order.
    /// </summary>
    IEnumerable<SharedType> GetSharedTypes();
}
