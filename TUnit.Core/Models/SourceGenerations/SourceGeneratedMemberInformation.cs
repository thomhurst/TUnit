using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TUnit.Core;

public abstract record SourceGeneratedMemberInformation
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public abstract Type Type { get; init; }

    public required string Name { get; init; }

    public required EqualityList<Attribute> Attributes { get; init; }
    
    protected virtual bool PrintMembers(StringBuilder stringBuilder)
    {
        stringBuilder.Append($"Type = {Type.Name}, ");
        stringBuilder.Append($"Name = {Name}");

        return true;
    }

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