namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of a property with a data source.
/// Contains only strings and primitives - no Roslyn symbols.
/// Used for both instance and static property injection.
/// </summary>
public sealed class PropertyDataModel : IEquatable<PropertyDataModel>
{
    // Property identity
    public required string PropertyName { get; init; }
    public required string PropertyTypeName { get; init; }
    public required string ContainingTypeName { get; init; }
    public required string MinimalContainingTypeName { get; init; }
    public required string Namespace { get; init; }
    public required string AssemblyName { get; init; }

    // Property characteristics
    public required bool IsStatic { get; init; }
    public required bool HasPublicGetter { get; init; }
    public required bool HasPublicSetter { get; init; }

    // Data source
    public required DataSourceModel DataSource { get; init; }

    // Attributes
    public required EquatableArray<ExtractedAttribute> PropertyAttributes { get; init; }

    public bool Equals(PropertyDataModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return PropertyName == other.PropertyName
               && ContainingTypeName == other.ContainingTypeName
               && IsStatic == other.IsStatic
               && DataSource.Equals(other.DataSource);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PropertyDataModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = PropertyName.GetHashCode();
            hash = (hash * 397) ^ ContainingTypeName.GetHashCode();
            hash = (hash * 397) ^ IsStatic.GetHashCode();
            hash = (hash * 397) ^ DataSource.GetHashCode();
            return hash;
        }
    }
}
