using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name})")]
public record SourceGeneratedParameterInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.PublicProperties)]T>() : SourceGeneratedParameterInformation(typeof(T));

[DebuggerDisplay("{Type} {Name})")]
public record SourceGeneratedParameterInformation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.PublicProperties)] Type Type) : SourceGeneratedMemberInformation
{
    public required ParameterInfo ReflectionInfo { get; set; }
    public bool IsParams => ReflectionInfo.IsDefined(typeof(ParamArrayAttribute), false);
    public bool IsOptional => ReflectionInfo.IsOptional;

    public object? DefaultValue => ReflectionInfo.DefaultValue;
}
