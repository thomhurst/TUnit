using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public class SourceGeneratedClassInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : SourceGeneratedClassInformation
{
    public override Type Type { get; } = typeof(T);
}

public abstract class SourceGeneratedClassInformation : SourceGeneratedMemberInformation
{
    public required SourceGeneratedParameterInformation[] Parameters { get; init; }
    
    public required SourceGeneratedPropertyInformation[] Properties { get; init; }
}