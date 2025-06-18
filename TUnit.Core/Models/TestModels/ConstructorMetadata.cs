using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace TUnit.Core;

[DebuggerDisplay("{Type} Constructor({Parameters.Length} parameters)")]
public record ConstructorMetadata : MemberMetadata
{
    public required ParameterMetadata[] Parameters { get; init; }

    public required bool IsStatic { get; init; }

    public required bool IsPublic { get; init; }

    public required bool IsPrivate { get; init; }

    public required bool IsProtected { get; init; }

    public required bool IsInternal { get; init; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override required Type Type { get; init; }

    [field: AllowNull, MaybeNull]
    [JsonIgnore]
    public ConstructorInfo ReflectionInformation
    {
        get => field ??= GetConstructorInfo();
        init => field = value;
    }

    private ConstructorInfo GetConstructorInfo()
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.Static;

        if (IsPublic) bindingFlags |= BindingFlags.Public;
        if (IsPrivate || IsProtected || IsInternal) bindingFlags |= BindingFlags.NonPublic;

        var constructors = Type.GetConstructors(bindingFlags);
        var parameterTypes = Parameters.Select(p => p.Type).ToArray();

        return constructors.FirstOrDefault(c =>
            c.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
            ?? throw new InvalidOperationException($"Constructor not found on type {Type} with parameters: {string.Join(", ", parameterTypes.Select(t => t.Name))}");
    }


    protected override bool PrintMembers(StringBuilder stringBuilder)
    {
        stringBuilder.Append($"Type = {Type.Name}, ");
        stringBuilder.Append($"Parameters = [{string.Join(", ", Parameters.Select(p => p.Type.Name))}], ");
        stringBuilder.Append($"IsPublic = {IsPublic}, ");
        stringBuilder.Append($"IsStatic = {IsStatic}");
        return true;
    }

    public virtual bool Equals(ConstructorMetadata? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other) &&
               MatchingParameters(other) &&
               IsStatic == other.IsStatic &&
               IsPublic == other.IsPublic &&
               IsPrivate == other.IsPrivate &&
               IsProtected == other.IsProtected &&
               IsInternal == other.IsInternal;
    }

    private bool MatchingParameters(ConstructorMetadata other)
    {
        return Parameters.Cast<ParameterMetadata>().SequenceEqual(other.Parameters);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * 397) ^ Parameters.GetHashCode();
            hashCode = (hashCode * 397) ^ IsStatic.GetHashCode();
            hashCode = (hashCode * 397) ^ IsPublic.GetHashCode();
            hashCode = (hashCode * 397) ^ IsPrivate.GetHashCode();
            hashCode = (hashCode * 397) ^ IsProtected.GetHashCode();
            hashCode = (hashCode * 397) ^ IsInternal.GetHashCode();
            return hashCode;
        }
    }
}
