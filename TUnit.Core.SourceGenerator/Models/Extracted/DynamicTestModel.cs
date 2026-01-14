namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of a dynamic test source method.
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class DynamicTestModel : IEquatable<DynamicTestModel>
{
    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string Namespace { get; init; }
    public required string MethodName { get; init; }
    public required bool IsStatic { get; init; }
    public required bool IsAsync { get; init; }
    public required string ReturnType { get; init; }

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

        return FullyQualifiedTypeName == other.FullyQualifiedTypeName
               && MethodName == other.MethodName
               && IsStatic == other.IsStatic;
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
            return hash;
        }
    }
}
