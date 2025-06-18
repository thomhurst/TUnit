using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[DebuggerDisplay("{Type}")]
public record ClassMetadata : MemberMetadata
{
    private static readonly ConcurrentDictionary<string, ClassMetadata> Cache = [];

    public static ClassMetadata GetOrAdd(string name, Func<ClassMetadata> factory)
    {
        return Cache.GetOrAdd(name, _ => factory());
    }

    public virtual bool Equals(ClassMetadata? other)
    {
        return Type == other?.Type;
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode();
    }

    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override required Type Type { get; init; }

    public required string? Namespace { get; init;}
    public required AssemblyMetadata Assembly { get; init; }
    public required ParameterMetadata[] Parameters { get; init; }

    public required PropertyMetadata[] Properties { get; init; }
    public required ConstructorMetadata[] Constructors { get; init; }
    public required ClassMetadata? Parent { get; init; }
}
