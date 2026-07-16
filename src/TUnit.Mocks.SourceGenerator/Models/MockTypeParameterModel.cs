using System;

namespace TUnit.Mocks.SourceGenerator.Models;

internal sealed record MockTypeParameterModel : IEquatable<MockTypeParameterModel>
{
    public string Name { get; init; } = "";
    public string Constraints { get; init; } = "";
    public bool HasReferenceTypeConstraint { get; init; }
    public bool HasValueTypeConstraint { get; init; }
    public bool HasAnnotatedNullableUsage { get; init; }

    public bool Equals(MockTypeParameterModel? other)
    {
        if (other is null) return false;
        return Name == other.Name
            && Constraints == other.Constraints
            && HasReferenceTypeConstraint == other.HasReferenceTypeConstraint
            && HasValueTypeConstraint == other.HasValueTypeConstraint
            && HasAnnotatedNullableUsage == other.HasAnnotatedNullableUsage;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Name.GetHashCode();
            hash = hash * 31 + Constraints.GetHashCode();
            hash = hash * 31 + HasReferenceTypeConstraint.GetHashCode();
            hash = hash * 31 + HasValueTypeConstraint.GetHashCode();
            hash = hash * 31 + HasAnnotatedNullableUsage.GetHashCode();
            return hash;
        }
    }
}
