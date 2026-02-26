using System;

namespace TUnit.Mocks.SourceGenerator.Models;

internal sealed record MockParameterModel : IEquatable<MockParameterModel>
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string FullyQualifiedType { get; init; } = "";
    public ParameterDirection Direction { get; init; } = ParameterDirection.In;
    public bool HasDefaultValue { get; init; }
    public string? DefaultValueExpression { get; init; }
    public bool IsValueType { get; init; }
    public bool IsRefStruct { get; init; }

    /// <summary>
    /// For ReadOnlySpan&lt;T&gt; or Span&lt;T&gt; parameters, the fully qualified element type (e.g. "byte").
    /// Null for non-span parameters. Used to support out/ref span parameters via array conversion.
    /// </summary>
    public string? SpanElementType { get; init; }

    public bool Equals(MockParameterModel? other)
    {
        if (other is null) return false;
        return Name == other.Name
            && Type == other.Type
            && FullyQualifiedType == other.FullyQualifiedType
            && Direction == other.Direction
            && IsValueType == other.IsValueType
            && IsRefStruct == other.IsRefStruct
            && SpanElementType == other.SpanElementType;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Name.GetHashCode();
            hash = hash * 31 + Type.GetHashCode();
            hash = hash * 31 + (int)Direction;
            hash = hash * 31 + IsValueType.GetHashCode();
            hash = hash * 31 + IsRefStruct.GetHashCode();
            hash = hash * 31 + (SpanElementType?.GetHashCode() ?? 0);
            return hash;
        }
    }
}

internal enum ParameterDirection
{
    In,
    Out,
    Ref,
    In_Readonly // in keyword
}
