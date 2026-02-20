using System;

namespace TUnit.Mock.SourceGenerator.Models;

internal sealed record MockParameterModel : IEquatable<MockParameterModel>
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string FullyQualifiedType { get; init; } = "";
    public ParameterDirection Direction { get; init; } = ParameterDirection.In;
    public bool HasDefaultValue { get; init; }
    public string? DefaultValueExpression { get; init; }

    public bool Equals(MockParameterModel? other)
    {
        if (other is null) return false;
        return Name == other.Name
            && Type == other.Type
            && FullyQualifiedType == other.FullyQualifiedType
            && Direction == other.Direction;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Name.GetHashCode();
            hash = hash * 31 + Type.GetHashCode();
            hash = hash * 31 + (int)Direction;
            return hash;
        }
    }
}

internal enum ParameterDirection
{
    In,
    Out,
    Ref,
    In_Readonly // in keyword
}
