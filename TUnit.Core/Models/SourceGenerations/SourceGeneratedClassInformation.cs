using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record SourceGeneratedClassInformation : SourceGeneratedMemberInformation
{
    private static readonly Dictionary<string, SourceGeneratedClassInformation> Cache = [];
    public static SourceGeneratedClassInformation GetOrAdd(string name, Func<SourceGeneratedClassInformation> factory)
    {
        if (Cache.TryGetValue(name, out var value))
        {
            return value;
        }
        
        value = factory();
        Cache[name] = value;
        return value;
    }
    
    public virtual bool Equals(SourceGeneratedClassInformation? other)
    {
        return Namespace == other?.Namespace 
               && Assembly.Equals(other.Assembly)
               && base.Equals(other);
    }
    
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Namespace.GetHashCode();
            hashCode = (hashCode * 397) ^ Assembly.GetHashCode();
            return hashCode;
        }
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public override required Type Type { get; init; }

    public required string Namespace {get; init;}
    public required SourceGeneratedAssemblyInformation Assembly { get; init; }
    public required SourceGeneratedParameterInformation[] Parameters { get; init; }
    
    public required SourceGeneratedPropertyInformation[] Properties { get; init; }
}