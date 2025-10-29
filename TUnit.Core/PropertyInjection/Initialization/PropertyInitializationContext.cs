using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core.PropertyInjection.Initialization;

/// <summary>
/// Encapsulates all context needed for property initialization.
/// Follows Single Responsibility Principle by being a pure data container.
/// </summary>
internal sealed class PropertyInitializationContext
{
    /// <summary>
    /// The object instance whose properties are being initialized.
    /// </summary>
    public required object Instance { get; init; }

    /// <summary>
    /// Property metadata for source-generated mode.
    /// </summary>
    public PropertyInjectionMetadata? SourceGeneratedMetadata { get; init; }

    /// <summary>
    /// Property info and data source for reflection mode.
    /// </summary>
    public PropertyInfo? PropertyInfo { get; init; }

    /// <summary>
    /// Data source attribute for the property.
    /// </summary>
    public IDataSourceAttribute? DataSource { get; init; }

    /// <summary>
    /// Property name being initialized.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Property type.
    /// </summary>
    public required Type PropertyType { get; init; }

    /// <summary>
    /// Action to set the property value.
    /// </summary>
    public required Action<object, object?> PropertySetter { get; init; }

    /// <summary>
    /// Shared object bag for the test context.
    /// </summary>
    public required ConcurrentDictionary<string, object?> ObjectBag { get; init; }

    /// <summary>
    /// Method metadata for the test.
    /// </summary>
    public MethodMetadata? MethodMetadata { get; init; }

    /// <summary>
    /// Test context events for tracking.
    /// </summary>
    public required TestContextEvents Events { get; init; }

    /// <summary>
    /// Visited objects for cycle detection.
    /// </summary>
    public required ConcurrentDictionary<object, byte> VisitedObjects { get; init; }

    /// <summary>
    /// Current test context (optional).
    /// </summary>
    public TestContext? TestContext { get; init; }

    /// <summary>
    /// The resolved value for the property (set during processing).
    /// </summary>
    public object? ResolvedValue { get; set; }

    /// <summary>
    /// Indicates if this is for nested property initialization.
    /// </summary>
    public bool IsNestedProperty { get; init; }

    /// <summary>
    /// Parent object for nested properties.
    /// </summary>
    public object? ParentInstance { get; init; }
}