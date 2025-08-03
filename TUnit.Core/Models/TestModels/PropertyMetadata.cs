using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name})")]
public record PropertyMetadata : MemberMetadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override required Type Type { get; init; }

    public required PropertyInfo ReflectionInfo { get; init; }

    public required bool IsStatic { get; init; }
    public bool IsNullable { get; init; }
    public required Func<object?, object?> Getter { get; init; }
    public ClassMetadata? ClassMetadata { get; set; }
}
