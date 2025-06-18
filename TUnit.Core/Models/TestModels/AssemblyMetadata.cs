using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[Obsolete]
public record SourceGeneratedAssemblyInformation : AssemblyMetadata;

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

    public required Attribute[] Attributes { get; init; }

    [field: AllowNull, MaybeNull] 
    public AttributeMetadata[] TestAttributes => field ??= Helpers.TestAttributeHelper.ConvertToTestAttributes(
        Attributes,
        TestAttributeTarget.Assembly,
        Name);
}
