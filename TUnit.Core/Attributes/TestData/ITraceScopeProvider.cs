namespace TUnit.Core;

/// <summary>
/// Implemented by data source attributes that expose <see cref="SharedType"/> information.
/// Used by the engine to parent initialization spans under the correct activity.
/// </summary>
internal interface ITraceScopeProvider
{
    /// <summary>
    /// Returns the <see cref="SharedType"/> for each generated object, in parameter order.
    /// </summary>
    IEnumerable<SharedType> GetSharedTypes();
}
