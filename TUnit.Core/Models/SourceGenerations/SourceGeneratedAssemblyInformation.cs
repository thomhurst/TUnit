namespace TUnit.Core;

public record SourceGeneratedAssemblyInformation
{
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