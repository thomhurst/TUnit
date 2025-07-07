using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name})")]
public record ParameterMetadata<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
T>() : ParameterMetadata(typeof(T));

[DebuggerDisplay("{Type} {Name})")]
public record ParameterMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.PublicProperties)] Type Type) : MemberMetadata
{
    public required TypeReference TypeReference { get; init; }
    public required ParameterInfo ReflectionInfo { get; set; }
    public bool IsParams => ReflectionInfo.IsDefined(typeof(ParamArrayAttribute), false);
    public bool IsOptional => ReflectionInfo.IsOptional;

    public object? DefaultValue => ReflectionInfo.DefaultValue;
}
