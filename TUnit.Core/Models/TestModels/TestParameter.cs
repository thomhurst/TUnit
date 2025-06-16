using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name})")]
public record TestParameter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.PublicProperties)]T>() : TestParameter(typeof(T));

[DebuggerDisplay("{Type} {Name})")]
public record TestParameter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.PublicProperties)] Type Type) : TestMember
{
    public required ParameterInfo ReflectionInfo { get; set; }
    public bool IsParams => ReflectionInfo.IsDefined(typeof(ParamArrayAttribute), false);
    public bool IsOptional => ReflectionInfo.IsOptional;

    public object? DefaultValue => ReflectionInfo.DefaultValue;
}
