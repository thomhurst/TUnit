namespace TUnit.Engine.Discovery;

/// <summary>
/// Registers hooks into Sources collections based on the execution mode (source generation or reflection).
/// </summary>
internal interface IHookRegistrar
{
    /// <summary>
    /// Discovers and registers all hooks for the test session into Sources collections.
    /// </summary>
    void DiscoverHooks();

    /// <summary>
    /// Discovers and registers instance hooks for a specific type (used for closed generic types).
    /// </summary>
    void DiscoverInstanceHooksForType(Type type);
}
