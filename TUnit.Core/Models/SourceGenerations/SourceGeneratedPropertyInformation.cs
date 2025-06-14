using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name})")]
public record SourceGeneratedPropertyInformation : SourceGeneratedMemberInformation
{
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override required Type Type { get; init; }

    public required PropertyInfo ReflectionInfo { get; init; }

    public required bool IsStatic { get; init; }
    public SharedType Shared { get; init; } = SharedType.None;
    public string? Key { get; init; } = null;
    public required Func<object?, object?> Getter { get; init; }
}
