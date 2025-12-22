using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name}")]
public record ParameterMetadata<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties)]
T>() : ParameterMetadata(typeof(T));

[DebuggerDisplay("{Type} {Name}")]
public record ParameterMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.NonPublicConstructors
    | DynamicallyAccessedMemberTypes.PublicProperties)] Type Type) : IMemberMetadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type Type { get; init; } = Type;

    public required string Name { get; init; }

    public required TypeInfo TypeInfo { get; init; }
    public required ParameterInfo ReflectionInfo { get; set; }
    public bool IsParams => CachedIsParams ?? ReflectionInfo.IsDefined(typeof(ParamArrayAttribute), false);
    public bool IsOptional => CachedIsOptional ?? ReflectionInfo.IsOptional;
    public bool IsNullable { get; init; }

    public object? DefaultValue => CachedDefaultValue ?? ReflectionInfo.DefaultValue;

    // AOT-friendly properties that avoid ParameterInfo access
    // Set by source generator to avoid reflection

    /// <summary>
    /// Cached IsParams value to avoid reflection call.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool? CachedIsParams { get; internal init; }

    /// <summary>
    /// Cached IsOptional value to avoid reflection call.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool? CachedIsOptional { get; internal init; }

    /// <summary>
    /// Cached default value to avoid reflection call.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public object? CachedDefaultValue { get; internal init; }

    /// <summary>
    /// Cached data source attributes to avoid reflection call.
    /// Set by source generator for AOT compatibility.
    /// When null, falls back to using ReflectionInfo.GetCustomAttributes().
    /// Includes attributes implementing IDataSourceAttribute or IDataSourceMemberAttribute.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Attribute[]? CachedDataSourceAttributes { get; internal init; }

    /// <summary>
    /// Position of this parameter in the method/constructor signature.
    /// </summary>
    public int Position { get; init; }
}
