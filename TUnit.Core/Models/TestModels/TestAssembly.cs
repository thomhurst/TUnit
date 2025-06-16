using System.Collections.Concurrent;
using System.Diagnostics;

namespace TUnit.Core;

[Obsolete]
public record SourceGeneratedAssemblyInformation : TestAssembly;

[DebuggerDisplay("{Name})")]
public record TestAssembly
{
    private static readonly ConcurrentDictionary<string, TestAssembly> Cache = [];
    public static TestAssembly GetOrAdd(string name, Func<TestAssembly> factory)
    {
        return Cache.GetOrAdd(name, _ => factory());
    }

    public virtual bool Equals(TestAssembly? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public required string Name { get; init; }

    public required Attribute[] Attributes { get; init; }
}
