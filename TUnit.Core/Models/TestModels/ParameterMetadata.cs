using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name}")]
public record ParameterMetadata<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
T>() : ParameterMetadata(typeof(T));

[DebuggerDisplay("{Type} {Name}")]
public record ParameterMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.NonPublicConstructors
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.NonPublicMethods
    | DynamicallyAccessedMemberTypes.PublicProperties)] Type Type) : MemberMetadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type Type { get; init; } = Type;

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
    public bool? CachedIsParams { get; init; }

    /// <summary>
    /// Cached IsOptional value to avoid reflection call.
    /// </summary>
    public bool? CachedIsOptional { get; init; }

    /// <summary>
    /// Cached default value to avoid reflection call.
    /// </summary>
    public object? CachedDefaultValue { get; init; }

    /// <summary>
    /// Position of this parameter in the method/constructor signature.
    /// </summary>
    public int Position { get; init; }
}
