using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[Obsolete]
public record SourceGeneratedPropertyInformation : PropertyMetadata;

[DebuggerDisplay("{Type} {Name})")]
public record PropertyMetadata : MemberMetadata
{
    [DynamicallyAccessedMembers(
        )]
    public override required Type Type { get; init; }

    public required PropertyInfo ReflectionInfo { get; init; }

    public required bool IsStatic { get; init; }
    public SharedType Shared { get; init; } = SharedType.None;
    public string? Key { get; init; } = null;
    public required Func<object?, object?> Getter { get; init; }
    public ClassMetadata? ClassMetadata { get; set; }
}
