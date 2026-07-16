namespace TUnit.Engine.Discovery;

/// <summary>
/// Hook registrar for source generation mode.
/// In this mode, hooks are registered at compile time via source generators, so no runtime registration is needed.
/// This implementation is AOT-compatible and does not use reflection.
/// </summary>
internal sealed class SourceGenHookRegistrar : IHookRegistrar
{
    /// <summary>
    /// No-op implementation. Hooks are already registered via source generation.
    /// </summary>
    public void DiscoverHooks()
    {
        // Hooks are already discovered and registered at compile time via source generation
        // No runtime discovery needed
    }

    /// <summary>
    /// No-op implementation. Instance hooks for generic types are already registered via source generation.
    /// </summary>
    public void DiscoverInstanceHooksForType(Type type)
    {
        // Hooks are already discovered and registered at compile time via source generation
        // No runtime discovery needed for closed generic types
    }
}
