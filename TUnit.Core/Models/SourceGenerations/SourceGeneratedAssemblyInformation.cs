using System.Collections.Concurrent;
using System.Diagnostics;

namespace TUnit.Core;

[DebuggerDisplay("{Name})")]
public record SourceGeneratedAssemblyInformation
{
    private static readonly ConcurrentDictionary<string, SourceGeneratedAssemblyInformation> Cache = [];
    public static SourceGeneratedAssemblyInformation GetOrAdd(string name, Func<SourceGeneratedAssemblyInformation> factory)
    {
        return Cache.GetOrAdd(name, _ => factory());
    }
    
    public virtual bool Equals(SourceGeneratedAssemblyInformation? other)
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