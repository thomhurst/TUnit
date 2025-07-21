using System.Collections.Concurrent;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

/// <summary>
/// Registry for property injection sources generated at compile time
/// </summary>
public static class PropertySourceRegistry
{
    private static readonly ConcurrentDictionary<Type, IPropertySource> _sources = new();

    /// <summary>
    /// Registers a property source for a type. Called by generated code.
    /// </summary>
    public static void Register(Type type, IPropertySource source)
    {
        _sources[type] = source;
    }

    /// <summary>
    /// Gets a property source for the given type
    /// </summary>
    public static IPropertySource? GetSource(Type type)
    {
        return _sources.TryGetValue(type, out var source) ? source : null;
    }
}