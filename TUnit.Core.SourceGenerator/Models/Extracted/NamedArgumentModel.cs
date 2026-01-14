namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of a named attribute argument.
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class NamedArgumentModel : IEquatable<NamedArgumentModel>
{
    public required string Name { get; init; }
    public required TypedConstantModel Value { get; init; }

    public bool Equals(NamedArgumentModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name && Value.Equals(other.Value);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as NamedArgumentModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Name.GetHashCode() * 397) ^ Value.GetHashCode();
        }
    }
}
