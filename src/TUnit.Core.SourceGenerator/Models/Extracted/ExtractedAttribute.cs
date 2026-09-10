using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of an AttributeData.
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class ExtractedAttribute : IEquatable<ExtractedAttribute>
{
    public required string FullyQualifiedName { get; init; }
    public required string MinimalName { get; init; }
    public required EquatableArray<TypedConstantModel> ConstructorArguments { get; init; }
    public required EquatableArray<NamedArgumentModel> NamedArguments { get; init; }

    public bool Equals(ExtractedAttribute? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FullyQualifiedName == other.FullyQualifiedName
               && MinimalName == other.MinimalName
               && ConstructorArguments.Equals(other.ConstructorArguments)
               && NamedArguments.Equals(other.NamedArguments);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ExtractedAttribute);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = FullyQualifiedName.GetHashCode();
            hash = (hash * 397) ^ MinimalName.GetHashCode();
            hash = (hash * 397) ^ ConstructorArguments.GetHashCode();
            hash = (hash * 397) ^ NamedArguments.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Extract an ExtractedAttribute from a Roslyn AttributeData.
    /// All symbol access happens here - the returned model contains only primitives.
    /// </summary>
    public static ExtractedAttribute Extract(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;

        var constructorArgs = attribute.ConstructorArguments
            .Select(TypedConstantModel.Extract)
            .ToEquatableArray();

        var namedArgs = attribute.NamedArguments
            .Select(kvp => new NamedArgumentModel
            {
                Name = kvp.Key,
                Value = TypedConstantModel.Extract(kvp.Value)
            })
            .ToEquatableArray();

        return new ExtractedAttribute
        {
            FullyQualifiedName = attributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "unknown",
            MinimalName = attributeClass?.Name ?? "unknown",
            ConstructorArguments = constructorArgs,
            NamedArguments = namedArgs
        };
    }

    /// <summary>
    /// Extract multiple attributes from an ISymbol.
    /// </summary>
    public static EquatableArray<ExtractedAttribute> ExtractAll(ISymbol symbol)
    {
        return symbol.GetAttributes()
            .Select(Extract)
            .ToEquatableArray();
    }

    /// <summary>
    /// Extract multiple attributes from an ImmutableArray.
    /// </summary>
    public static EquatableArray<ExtractedAttribute> ExtractAll(IEnumerable<AttributeData> attributes)
    {
        return attributes
            .Select(Extract)
            .ToEquatableArray();
    }

    /// <summary>
    /// Gets a constructor argument by index, or null if index is out of range.
    /// </summary>
    public TypedConstantModel? GetConstructorArgument(int index)
    {
        return index >= 0 && index < ConstructorArguments.Length
            ? ConstructorArguments[index]
            : null;
    }
}
