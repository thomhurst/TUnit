namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of a dynamic test source method.
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class DynamicTestModel : IEquatable<DynamicTestModel>
{
    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string MethodName { get; init; }
    public required bool IsStatic { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }

    public bool Equals(DynamicTestModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Every field feeds the generated source (FilePath and LineNumber also feed the hint name),
        // so all of them must be compared.
        return FullyQualifiedTypeName == other.FullyQualifiedTypeName
               && MinimalTypeName == other.MinimalTypeName
               && MethodName == other.MethodName
               && IsStatic == other.IsStatic
               && FilePath == other.FilePath
               && LineNumber == other.LineNumber;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DynamicTestModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = FullyQualifiedTypeName.GetHashCode();
            hash = (hash * 397) ^ MethodName.GetHashCode();
            hash = (hash * 397) ^ IsStatic.GetHashCode();
            hash = (hash * 397) ^ FilePath.GetHashCode();
            hash = (hash * 397) ^ LineNumber;
            return hash;
        }
    }
}
