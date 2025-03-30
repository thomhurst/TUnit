using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using TUnit.Core.Helpers;

namespace TUnit.Core;

public record SourceGeneratedMethodInformation : SourceGeneratedMemberInformation
{
    internal static SourceGeneratedMethodInformation Failure<TClassType>(string methodName) =>
        new()
        {
            Attributes = [],
            Name = methodName,
            ReturnType = typeof(void),
            Type = typeof(TClassType),
            Parameters = [],
            GenericTypeCount = 0,
            Class = new SourceGeneratedClassInformation
            {
                Assembly = new SourceGeneratedAssemblyInformation
                {
                    Attributes = [],
                    Name = typeof(TClassType).Assembly.GetName().Name!,
                },
                Attributes = [],
                Name = typeof(TClassType).Name,
                Namespace = typeof(TClassType).Namespace,
                Parameters = [],
                Properties = [],
                Type = typeof(TClassType)
            }
        };

    public required SourceGeneratedParameterInformation[] Parameters { get; init; }

    public required int GenericTypeCount { get; init; }

    public required SourceGeneratedClassInformation Class { get; init; }

    [field: AllowNull, MaybeNull]
    [JsonIgnore]
    public MethodInfo ReflectionInformation
    {
        [RequiresUnreferencedCode("Reflection API")]
        get => field ??=
            MethodInfoRetriever.GetMethodInfo(Type, Name, GenericTypeCount, Parameters.Select(x => x.Type).ToArray());
        set;
    }

    public required Type ReturnType { get; init; }

    public override required Type Type { get; init; }
    
    protected override bool PrintMembers(StringBuilder stringBuilder)
    {
        stringBuilder.Append($"ReturnType = {ReturnType.Name}, ");
        stringBuilder.Append($"GenericTypeCount = {GenericTypeCount}, ");
        stringBuilder.Append($"Type = {Type.Name}, ");
        stringBuilder.Append($"Name = {Name}");
        return true;
    }

    public virtual bool Equals(SourceGeneratedMethodInformation? other)
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