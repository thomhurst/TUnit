using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[Obsolete]
public record SourceGeneratedMethodInformation : TestMethod;

[DebuggerDisplay("{Type}.{Name}")]
public record TestMethod : TestMember
{
    internal static TestMethod Failure< [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.NonPublicMethods)] TClassType>(string methodName) =>
        new()
        {
            Attributes = [],
            Name = methodName,
            ReturnType = typeof(void),
            Type = typeof(TClassType),
            Parameters = [],
            GenericTypeCount = 0,
            Class = new TestClass
            {
                Parent = null,
                Assembly = new TestAssembly
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

    public required TestParameter[] Parameters { get; init; }

    public required int GenericTypeCount { get; init; }

    public required TestClass Class { get; init; }

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

    public virtual bool Equals(TestMethod? other)
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
