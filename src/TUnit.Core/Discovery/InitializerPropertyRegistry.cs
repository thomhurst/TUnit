using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Discovery;

/// <summary>
/// Registry for IAsyncInitializer property metadata generated at compile time.
/// Used for AOT-compatible nested initializer discovery.
/// </summary>
public static class InitializerPropertyRegistry
{
    private static readonly ConcurrentDictionary<Type, InitializerPropertyInfo[]> Registry = new();

    /// <summary>
    /// Registers property metadata for a type. Called by generated code from a static field
    /// initializer on the consolidated registration <c>.cctor</c> (so N per-file module
    /// initializers collapse into one merged <c>.cctor</c>).
    /// Returns a dummy value for use as a static field initializer.
    /// </summary>
    public static int Register(Type type, InitializerPropertyInfo[] properties)
    {
        Registry[type] = properties;
        return 0;
    }

    /// <summary>
    /// Gets property metadata for a type, or null if not registered.
    /// </summary>
    public static InitializerPropertyInfo[]? GetProperties(Type type)
    {
        return Registry.TryGetValue(type, out var properties) ? properties : null;
    }

    /// <summary>
    /// Checks if a type has registered property metadata.
    /// </summary>
    public static bool HasRegistration(Type type)
    {
        return Registry.ContainsKey(type);
    }
}

/// <summary>
/// Metadata about a property that returns an IAsyncInitializer.
/// </summary>
public sealed class InitializerPropertyInfo
{
    /// <summary>
    /// The name of the property.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// The property type.
    /// </summary>
    public required Type PropertyType { get; init; }

    /// <summary>
    /// Delegate to get the property value from an instance.
    /// </summary>
    public required Func<object, object?> GetValue { get; init; }
}
