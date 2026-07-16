namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// The type of data source.
/// </summary>
public enum DataSourceKind
{
    None,
    Arguments,
    MethodDataSource,
    ClassDataSource,
    DataSourceGenerator,
    Matrix,
    EnumDataSource
}

/// <summary>
/// Primitive representation of a data source attribute.
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class DataSourceModel : IEquatable<DataSourceModel>
{
    public required DataSourceKind Kind { get; init; }

    /// <summary>
    /// For MethodDataSource: the method name to call.
    /// </summary>
    public required string? MethodName { get; init; }

    /// <summary>
    /// For MethodDataSource/ClassDataSource: the type containing the method/data.
    /// </summary>
    public required string? ContainingTypeName { get; init; }

    /// <summary>
    /// For Arguments: the literal argument values.
    /// </summary>
    public required EquatableArray<TypedConstantModel> Arguments { get; init; }

    /// <summary>
    /// The full extracted attribute data.
    /// </summary>
    public required ExtractedAttribute SourceAttribute { get; init; }

    /// <summary>
    /// For DataSourceGenerator: the generator type name.
    /// </summary>
    public required string? GeneratorTypeName { get; init; }

    /// <summary>
    /// Whether this is a shared/keyed data source.
    /// </summary>
    public required bool IsShared { get; init; }

    /// <summary>
    /// The shared key if IsShared is true.
    /// </summary>
    public required string? SharedKey { get; init; }

    /// <summary>
    /// Index for ordering when multiple data sources exist.
    /// </summary>
    public required int Index { get; init; }

    public bool Equals(DataSourceModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Kind == other.Kind
               && MethodName == other.MethodName
               && ContainingTypeName == other.ContainingTypeName
               && Arguments.Equals(other.Arguments)
               && SourceAttribute.Equals(other.SourceAttribute)
               && GeneratorTypeName == other.GeneratorTypeName
               && IsShared == other.IsShared
               && SharedKey == other.SharedKey
               && Index == other.Index;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DataSourceModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int)Kind;
            hash = (hash * 397) ^ (MethodName?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (ContainingTypeName?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ Arguments.GetHashCode();
            hash = (hash * 397) ^ SourceAttribute.GetHashCode();
            hash = (hash * 397) ^ (GeneratorTypeName?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ IsShared.GetHashCode();
            hash = (hash * 397) ^ (SharedKey?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ Index;
            return hash;
        }
    }
}
