namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of assembly information for infrastructure generation.
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class AssemblyInfoModel : IEquatable<AssemblyInfoModel>
{
    /// <summary>
    /// Assembly names to pre-load at module initialization.
    /// </summary>
    public required EquatableArray<string> AssembliesToLoad { get; init; }

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

        return AssembliesToLoad.Equals(other.AssembliesToLoad);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as AssemblyInfoModel);
    }

    public override int GetHashCode()
    {
        return AssembliesToLoad.GetHashCode();
    }
}
