using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.PropertyTesting;

/// <summary>
/// Specifies how to generate random test data for a parameter in property-based testing.
/// Used in conjunction with <see cref="PropertyDataSourceAttribute"/> to enable automatic test case generation and shrinking.
/// </summary>
/// <typeparam name="T">The type of values to generate for this parameter</typeparam>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class PropertyDataAttribute<T> : Attribute, IInfersType<T>
{
    /// <summary>
    /// Gets or sets the custom generator type to use for this parameter.
    /// Must implement <see cref="IGenerator{T}"/>.
    /// If not specified, the default generator for type T will be used.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type? GeneratorType { get; init; }

    /// <summary>
    /// Gets or sets the custom shrinker type to use for this parameter.
    /// Must implement <see cref="IShrinker{T}"/>.
    /// If not specified, the default shrinker for type T will be used.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type? ShrinkerType { get; init; }

    /// <summary>
    /// Gets or sets the minimum value for numeric types.
    /// Only applicable for numeric generators (int, long, double, etc.).
    /// </summary>
    public object? Min { get; init; }

    /// <summary>
    /// Gets or sets the maximum value for numeric types.
    /// Only applicable for numeric generators (int, long, double, etc.).
    /// </summary>
    public object? Max { get; init; }

    /// <summary>
    /// Gets or sets the minimum length for collection or string types.
    /// Set to -1 (default) to use type defaults.
    /// </summary>
    public int MinLength { get; init; } = -1;

    /// <summary>
    /// Gets or sets the maximum length for collection or string types.
    /// Set to -1 (default) to use type defaults.
    /// </summary>
    public int MaxLength { get; init; } = -1;

    /// <summary>
    /// Gets or sets whether to generate only positive values for numeric types.
    /// </summary>
    public bool PositiveOnly { get; init; }

    /// <summary>
    /// Gets or sets whether to allow null values for nullable reference types.
    /// Default is true for nullable reference types.
    /// </summary>
    public bool AllowNull { get; init; } = true;
}
