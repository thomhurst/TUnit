using System.Diagnostics.CodeAnalysis;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Hook discovery service for reflection mode.
/// Uses reflection to scan assemblies and discover hooks at runtime.
/// This implementation requires reflection and is NOT AOT-compatible.
/// </summary>
#if NET6_0_OR_GREATER
[RequiresUnreferencedCode("Hook discovery uses reflection to scan assemblies and types")]
[RequiresUnreferencedCode("Hook delegate creation requires dynamic code generation")]
#endif
internal sealed class ReflectionBasedHookDiscoveryService : IHookDiscoveryService
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
