namespace TUnit.Core;

public record SourceGeneratedAssemblyInformation
{
    private static readonly Dictionary<string, SourceGeneratedAssemblyInformation> Cache = [];
    public static SourceGeneratedAssemblyInformation GetOrAdd(string name, Func<SourceGeneratedAssemblyInformation> factory)
    {
        if (Cache.TryGetValue(name, out var value))
        {
            return value;
        }
        
        value = factory();
        Cache[name] = value;
        return value;
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