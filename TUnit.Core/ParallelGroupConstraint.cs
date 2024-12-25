using TUnit.Core.Interfaces;

namespace TUnit.Core;

public record ParallelGroupConstraint(string Group) : IParallelConstraint,
    IComparable<ParallelGroupConstraint>,
    IComparable
{
    public int CompareTo(ParallelGroupConstraint? other)
    {
        return string.Compare(Group, other?.Group, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        return CompareTo(obj as ParallelGroupConstraint);
    }
}