namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of assembly information for infrastructure generation.
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class AssemblyInfoModel : IEquatable<AssemblyInfoModel>
{
    /// <summary>
    /// Fully qualified type names to reference, which loads their assemblies and triggers hook discovery.
    /// </summary>
    public required EquatableArray<string> TypesToReference { get; init; }

    public bool Equals(AssemblyInfoModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return TypesToReference.Equals(other.TypesToReference);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as AssemblyInfoModel);
    }

    public override int GetHashCode()
    {
        return TypesToReference.GetHashCode();
    }
}
