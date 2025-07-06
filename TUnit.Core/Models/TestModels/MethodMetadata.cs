using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TUnit.Core;

[Obsolete]
public record SourceGeneratedMethodInformation : MethodMetadata;

[DebuggerDisplay("{Type}.{Name}")]
public record MethodMetadata : MemberMetadata
{
    public required ParameterMetadata[] Parameters { get; init; }

    public required int GenericTypeCount { get; init; }

    public required ClassMetadata Class { get; init; }


    public required TypeReference ReturnTypeReference { get; init; }

    /// <summary>
    /// The concrete return type (only available for non-generic types).
    /// For generic types, this will be null and ReturnTypeReference must be resolved at runtime.
    /// </summary>
    public Type? ReturnType { get; init; }

    public required TypeReference TypeReference { get; init; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override required Type Type { get; init; }

    protected override bool PrintMembers(StringBuilder stringBuilder)
    {
        stringBuilder.Append($"ReturnTypeReference = {ReturnTypeReference.AssemblyQualifiedName ?? "GenericParameter"},");
        stringBuilder.Append($"GenericTypeCount = {GenericTypeCount},");
        stringBuilder.Append($"TypeReference = {TypeReference.AssemblyQualifiedName ?? "GenericParameter"},");
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

        return base.Equals(other) && Parameters.SequenceEqual(other.Parameters) && GenericTypeCount == other.GenericTypeCount && Class.Equals(other.Class) && ReturnTypeReference.Equals(other.ReturnTypeReference) && TypeReference.Equals(other.TypeReference) && Type.Equals(other.Type);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * 397) ^ Parameters.GetHashCode();
            hashCode = (hashCode * 397) ^ GenericTypeCount;
            hashCode = (hashCode * 397) ^ Class.GetHashCode();
            hashCode = (hashCode * 397) ^ ReturnTypeReference.GetHashCode();
            hashCode = (hashCode * 397) ^ TypeReference.GetHashCode();
            hashCode = (hashCode * 397) ^ Type.GetHashCode();
            return hashCode;
        }
    }
}
