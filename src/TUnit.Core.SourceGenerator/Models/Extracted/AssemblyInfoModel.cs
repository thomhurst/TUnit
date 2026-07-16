namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of assembly information for infrastructure generation.
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class AssemblyInfoModel : IEquatable<AssemblyInfoModel>
{
    /// <summary>
    /// Name of the current assembly being generated for.
    /// Used for debugging to identify which assembly's module initializer is running.
    /// </summary>
    public required string AssemblyName { get; init; }

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

        return AssemblyName == other.AssemblyName && TypesToReference.Equals(other.TypesToReference);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as AssemblyInfoModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (AssemblyName?.GetHashCode() ?? 0);
            hash = hash * 31 + TypesToReference.GetHashCode();
            return hash;
        }
    }
}
