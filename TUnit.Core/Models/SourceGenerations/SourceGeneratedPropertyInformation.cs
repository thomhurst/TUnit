using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name})")]
public record SourceGeneratedPropertyInformation : SourceGeneratedMemberInformation
{
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public override required Type Type { get; init; }
    public required bool IsStatic { get; init; }
    public SharedType Shared { get; init; } = SharedType.None;
    public string? Key { get; init; } = null;
}