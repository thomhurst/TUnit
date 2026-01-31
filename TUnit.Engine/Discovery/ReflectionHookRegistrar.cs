using System.Diagnostics.CodeAnalysis;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Hook registrar for reflection mode.
/// Uses reflection to scan assemblies and register hooks into Sources at runtime.
/// This implementation requires reflection and is NOT AOT-compatible.
/// </summary>
#if NET6_0_OR_GREATER
[RequiresUnreferencedCode("Hook registration uses reflection to scan assemblies and types")]
#endif
internal sealed class ReflectionHookRegistrar : IHookRegistrar
{
    /// <summary>
    /// Discovers hooks using reflection by scanning all loaded assemblies.
    /// </summary>
    public void DiscoverHooks()
    {
        ReflectionHookDiscoveryService.DiscoverHooks();
    }

    /// <summary>
    /// Discovers instance hooks for a specific closed generic type using reflection.
    /// </summary>
    public void DiscoverInstanceHooksForType(Type type)
    {
        ReflectionHookDiscoveryService.DiscoverInstanceHooksForType(type);
    }
}
