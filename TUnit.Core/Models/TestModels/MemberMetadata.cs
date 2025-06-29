using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TUnit.Core;

[Obsolete]
public abstract record SourceGeneratedMemberInformation : MemberMetadata;

public abstract record MemberMetadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public abstract Type Type { get; init; }

    public required string Name { get; init; }

    public required AttributeMetadata[] Attributes { get; init; }

    protected virtual bool PrintMembers(StringBuilder stringBuilder)
    {
        stringBuilder.Append($"Type = {Type.Name},");
        stringBuilder.Append($"Name = {Name}");

        return true;
    }

    public virtual bool Equals(MemberMetadata? other)
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
