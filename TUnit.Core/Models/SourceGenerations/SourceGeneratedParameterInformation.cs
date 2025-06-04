using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name})")]
public record SourceGeneratedParameterInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.NonPublicMethods)]T>() : SourceGeneratedParameterInformation(typeof(T));

[DebuggerDisplay("{Type} {Name})")]
public record SourceGeneratedParameterInformation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type Type) : SourceGeneratedMemberInformation
{
    public required bool IsOptional { get; set; }
    public required object? DefaultValue { get; set; }
}
