using TUnit.Core.Interfaces;

namespace TUnit.Core;

public record ParallelGroupConstraint(string Group, int Order) : IParallelConstraint,
    IComparable<ParallelGroupConstraint>,
    IComparable
{
    public virtual bool Equals(ParallelGroupConstraint? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Group == other.Group;
    }

    public override int GetHashCode()
    {
        return Group.GetHashCode();
    }

    public int CompareTo(ParallelGroupConstraint? other)
    {
        return string.Compare(Group, other?.Group, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        return CompareTo(obj as ParallelGroupConstraint);
    }
}
