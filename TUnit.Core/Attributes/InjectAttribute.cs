using System;

namespace TUnit.Core;

/// <summary>
/// Marks a property for dependency injection during test execution.
/// Properties marked with this attribute will be injected before test execution
/// and disposed (if applicable) after test completion.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class InjectAttribute : Attribute
{
    /// <summary>
    /// Indicates whether this property is required.
    /// If true, test execution will fail if the property cannot be resolved.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Specifies a specific service key to use when resolving from keyed services.
    /// </summary>
    public object? ServiceKey { get; set; }

    /// <summary>
    /// Specifies the order in which properties should be injected.
    /// Lower values are injected first. Default is 0.
    /// </summary>
    public int Order { get; set; } = 0;
}