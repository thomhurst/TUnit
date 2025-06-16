using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[DebuggerDisplay("{Type}")]
public record TestClass : TestMember
{
    private static readonly ConcurrentDictionary<string, TestClass> Cache = [];

    public static TestClass GetOrAdd(string name, Func<TestClass> factory)
    {
        return Cache.GetOrAdd(name, _ => factory());
    }

    public virtual bool Equals(TestClass? other)
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
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override required Type Type { get; init; }

    public required string? Namespace { get; init;}
    public required TestAssembly Assembly { get; init; }
    public required TestParameter[] Parameters { get; init; }

    public required TestProperty[] Properties { get; init; }
    public required TestClass? Parent { get; init; }
}
