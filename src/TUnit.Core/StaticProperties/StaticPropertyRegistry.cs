using System.Collections.Concurrent;

namespace TUnit.Core.StaticProperties;

/// <summary>
/// Registry for static properties that need initialization before tests run.
/// Used by source-generated code to register metadata, and by the framework to initialize properties.
/// </summary>
public static class StaticPropertyRegistry
{
    private static readonly ConcurrentBag<StaticPropertyMetadata> _registeredProperties = [];
    private static readonly ConcurrentDictionary<string, object> _initializedValues = new();

    /// <summary>
    /// Register a static property for initialization. Called by generated code from a static field
    /// initializer on the consolidated registration <c>.cctor</c> (so the per-assembly module
    /// initializer collapses into one merged <c>.cctor</c>).
    /// Returns a dummy value for use as a static field initializer.
    /// </summary>
    public static int Register(StaticPropertyMetadata metadata)
    {
        _registeredProperties.Add(metadata);
        return 0;
    }

    /// <summary>
    /// Get all registered static properties
    /// </summary>
    public static IEnumerable<StaticPropertyMetadata> GetRegisteredProperties()
    {
        return _registeredProperties;
    }

    /// <summary>
    /// Get initialized value for a property (for tracking)
    /// </summary>
    public static bool TryGetInitializedValue(Type declaringType, string propertyName, out object? value)
    {
        var key = $"{declaringType.FullName}.{propertyName}";
        return _initializedValues.TryGetValue(key, out value);
    }

    /// <summary>
    /// Store initialized value for a property (for tracking)
    /// </summary>
    internal static void StoreInitializedValue(Type declaringType, string propertyName, object value)
    {
        var key = $"{declaringType.FullName}.{propertyName}";
        _initializedValues[key] = value;
    }

    /// <summary>
    /// Get all initialized values for tracking
    /// </summary>
    internal static IEnumerable<object> GetAllInitializedValues()
    {
        return _initializedValues.Values;
    }
}
