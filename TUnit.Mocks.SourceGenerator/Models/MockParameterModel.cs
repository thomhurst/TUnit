using System;

namespace TUnit.Mocks.SourceGenerator.Models;

internal sealed record MockParameterModel : IEquatable<MockParameterModel>
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string FullyQualifiedType { get; init; } = "";
    public ParameterDirection Direction { get; init; } = ParameterDirection.In;
    public bool HasDefaultValue { get; init; }
    public string? DefaultValueExpression { get; init; }
    public bool IsValueType { get; init; }

    public bool Equals(MockParameterModel? other)
    {
        if (other is null) return false;
        return Name == other.Name
            && Type == other.Type
            && FullyQualifiedType == other.FullyQualifiedType
            && Direction == other.Direction
            && IsValueType == other.IsValueType;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Name.GetHashCode();
            hash = hash * 31 + Type.GetHashCode();
            hash = hash * 31 + (int)Direction;
            hash = hash * 31 + IsValueType.GetHashCode();
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
