using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Interfaces.SourceGenerator;

/// <summary>
/// Metadata about a property that needs data source injection, generated at compile-time.
/// </summary>
public sealed class PropertyInjectionMetadata
{
    private Func<object, object?>? _getProperty;

    /// <summary>
    /// Gets the name of the property that needs injection.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Gets the type of the property.
    /// </summary>
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicProperties)]
    public required Type PropertyType { get; init; }

    /// <summary>
    /// Gets the type that contains the property (the parent class).
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    public required Type ContainingType { get; init; }

    /// <summary>
    /// Gets a factory function that creates the data source attribute instance.
    /// </summary>
    public required Func<IDataSourceAttribute> CreateDataSource { get; init; }

    /// <summary>
    /// Gets a delegate that sets the property value on the target instance.
    /// Handles type casting and property setting (including init-only properties).
    /// </summary>
    public required Action<object, object?> SetProperty { get; init; }

    /// <summary>
    /// Optional compile-time-generated getter that returns the property value from an instance.
    /// When not supplied (e.g. older source-gen output or hand-authored metadata) the callers
    /// fall back to a cached <see cref="PropertyInfo.GetValue(object)"/> call. Supplying a
    /// direct delegate removes a <see cref="Type.GetProperty(string)"/> reflection lookup from
    /// the per-test hot path in both source-gen and reflection modes.
    /// </summary>
    public Func<object, object?>? GetProperty
    {
        get => _getProperty;
        init => _getProperty = value;
    }

    /// <summary>
    /// Returns a delegate that reads the property value from a given instance. Uses the
    /// source-generated <see cref="GetProperty"/> delegate when available, otherwise compiles
    /// a reflection-based getter once and caches it on the metadata instance.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "ContainingType is annotated to preserve properties; reflection fallback is only used when source-gen did not emit a delegate.")]
    internal Func<object, object?> GetOrCreateGetter()
    {
        if (_getProperty != null)
        {
            return _getProperty;
        }

        var property = ContainingType.GetProperty(PropertyName);
        if (property is null || !property.CanRead)
        {
            // Memoize a stub that mirrors the behavior the previous reflection-based
            // call sites expected (null when the property can't be read).
            return _getProperty = static _ => null;
        }

        return _getProperty = property.GetValue;
    }
}
