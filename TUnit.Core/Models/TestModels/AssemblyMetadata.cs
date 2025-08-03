using System.Collections.Concurrent;
using System.Diagnostics;

namespace TUnit.Core;

[DebuggerDisplay("{Name})")]
public record AssemblyMetadata
{
    private static readonly ConcurrentDictionary<string, AssemblyMetadata> Cache = [];
    public static AssemblyMetadata GetOrAdd(string name, Func<AssemblyMetadata> factory)
    {
        return Cache.GetOrAdd(name, _ => factory());
    }

    public virtual bool Equals(AssemblyMetadata? other)
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
}
