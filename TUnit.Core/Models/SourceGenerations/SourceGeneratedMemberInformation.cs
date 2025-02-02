using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public abstract record SourceGeneratedMemberInformation
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public abstract Type Type { get; init; }

    public required string Name { get; init; }

    public required Attribute[] Attributes { get; init; }

    public virtual bool Equals(SourceGeneratedMemberInformation? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Type == other.Type && Name == other.Name;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Type.GetHashCode() * 397) ^ Name.GetHashCode();
        }
    }
}