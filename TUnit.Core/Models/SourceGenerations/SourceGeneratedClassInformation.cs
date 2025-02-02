using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record SourceGeneratedClassInformation : SourceGeneratedMemberInformation
{
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