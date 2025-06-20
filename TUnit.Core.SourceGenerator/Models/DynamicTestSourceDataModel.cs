using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

public record DynamicTestSourceDataModel
{
    public virtual bool Equals(DynamicTestSourceDataModel? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FilePath == other.FilePath
            && LineNumber == other.LineNumber;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + FilePath.GetHashCode();
            hash = hash * 31 + LineNumber.GetHashCode();
            return hash;
        }
    }
    
    public required INamedTypeSymbol Class { get; init; }
    public required IMethodSymbol Method { get; init; }
    
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
}