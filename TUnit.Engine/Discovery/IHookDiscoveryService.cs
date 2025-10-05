namespace TUnit.Engine.Discovery;

/// <summary>
/// Discovers hooks based on the execution mode (source generation or reflection).
/// </summary>
internal interface IHookDiscoveryService
{
    /// <summary>
    /// Discovers and registers all hooks for the test session.
    /// </summary>
    void DiscoverHooks();

    /// <summary>
    /// Discovers instance hooks for a specific type (used for closed generic types).
    /// </summary>
    void DiscoverInstanceHooksForType(Type type);
}
