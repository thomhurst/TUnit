using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TUnit.Core;

[DebuggerDisplay("{Type}.{Name}")]
public record MethodMetadata : IMemberMetadata
{
    public required ParameterMetadata[] Parameters { get; init; }

    public required int GenericTypeCount { get; init; }

    public required ClassMetadata Class { get; init; }

    public required TypeInfo ReturnTypeInfo { get; init; }

    /// <summary>
    /// The concrete return type (only available for non-generic types).
    /// For generic types, this will be null and ReturnTypeInfo must be resolved at runtime.
    /// </summary>
    public Type? ReturnType { get; init; }

    public required TypeInfo TypeInfo { get; init; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public required Type Type { get; init; }

    public required string Name { get; init; }

    protected virtual bool PrintMembers(StringBuilder stringBuilder)
    {
        stringBuilder.Append($"ReturnTypeInfo = {ReturnTypeInfo},");
        stringBuilder.Append($"GenericTypeCount = {GenericTypeCount},");
        stringBuilder.Append($"TypeInfo = {TypeInfo},");
        stringBuilder.Append($"Name = {Name}");
        return true;
    }

    public virtual bool Equals(MethodMetadata? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other) && Parameters.SequenceEqual(other.Parameters) && GenericTypeCount == other.GenericTypeCount && Class.Equals(other.Class) && ReturnTypeInfo.Equals(other.ReturnTypeInfo) && TypeInfo.Equals(other.TypeInfo) && Type.Equals(other.Type);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * 397) ^ Parameters.GetHashCode();
            hashCode = (hashCode * 397) ^ GenericTypeCount;
            hashCode = (hashCode * 397) ^ Class.GetHashCode();
            hashCode = (hashCode * 397) ^ ReturnTypeInfo.GetHashCode();
            hashCode = (hashCode * 397) ^ TypeInfo.GetHashCode();
            hashCode = (hashCode * 397) ^ Type.GetHashCode();
            return hashCode;
        }
    }
}
