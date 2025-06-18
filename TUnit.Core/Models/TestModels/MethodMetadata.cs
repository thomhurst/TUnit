using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[Obsolete]
public record SourceGeneratedMethodInformation : MethodMetadata;

[DebuggerDisplay("{Type}.{Name}")]
public record MethodMetadata : MemberMetadata
{
    public required ParameterMetadata[] Parameters { get; init; }

    public required int GenericTypeCount { get; init; }

    public required ClassMetadata Class { get; init; }

    [field: AllowNull, MaybeNull]
    [JsonIgnore]
    public MethodInfo ReflectionInformation
    {
        get => field ??=
            MethodInfoRetriever.GetMethodInfo(Type, Name, GenericTypeCount, Parameters.Select(x => x.Type).ToArray());
        set;
    }

    public required Type ReturnType { get; init; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public override required Type Type { get; init; }

    protected override bool PrintMembers(StringBuilder stringBuilder)
    {
        stringBuilder.Append($"ReturnType = {ReturnType.Name}, ");
        stringBuilder.Append($"GenericTypeCount = {GenericTypeCount}, ");
        stringBuilder.Append($"Type = {Type.Name}, ");
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

        return base.Equals(other) && Parameters.SequenceEqual(other.Parameters) && GenericTypeCount == other.GenericTypeCount && Class.Equals(other.Class) && ReturnType.Equals(other.ReturnType) && Type.Equals(other.Type);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * 397) ^ Parameters.GetHashCode();
            hashCode = (hashCode * 397) ^ GenericTypeCount;
            hashCode = (hashCode * 397) ^ Class.GetHashCode();
            hashCode = (hashCode * 397) ^ ReturnType.GetHashCode();
            hashCode = (hashCode * 397) ^ Type.GetHashCode();
            return hashCode;
        }
    }
}
