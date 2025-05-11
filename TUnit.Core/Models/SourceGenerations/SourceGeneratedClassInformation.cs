using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[DebuggerDisplay("{Type})")]
public record SourceGeneratedClassInformation : SourceGeneratedMemberInformation
{
    private static readonly ConcurrentDictionary<string, SourceGeneratedClassInformation> Cache = [];
    
    public static SourceGeneratedClassInformation GetOrAdd(string name, Func<SourceGeneratedClassInformation> factory)
    {
        return Cache.GetOrAdd(name, _ => factory());
    }

    public virtual bool Equals(SourceGeneratedClassInformation? other)
    {
        return Type == other?.Type;
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode();
    }

    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public override required Type Type { get; init; }

    public required string? Namespace { get; init;}
    public required SourceGeneratedAssemblyInformation Assembly { get; init; }
    public required SourceGeneratedParameterInformation[] Parameters { get; init; }
    
    public required SourceGeneratedPropertyInformation[] Properties { get; init; }
}