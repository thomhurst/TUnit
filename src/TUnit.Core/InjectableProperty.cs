using System.ComponentModel;

namespace TUnit.Core;

/// <summary>
/// Describes a property on a test class that has an IDataSourceAttribute and needs injection.
/// Source-generated: the source generator identifies these at compile time and emits the setter delegate,
/// eliminating reflection-based property discovery at runtime.
/// </summary>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class InjectableProperty
{
    /// <summary>
    /// The property name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The property type.
    /// </summary>
    public required Type Type { get; init; }

    /// <summary>
    /// The data source attribute on this property.
    /// </summary>
    public required IDataSourceAttribute DataSource { get; init; }

    /// <summary>
    /// AOT-safe setter delegate. Calls the property setter without reflection.
    /// Signature: (object instance, object? value) — the instance is cast internally.
    /// </summary>
    public required Action<object, object?> SetValue { get; init; }
}
