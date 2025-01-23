using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public class SourceGeneratedTestInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : SourceGeneratedTestInformation
{
    public override Type Type { get; } = typeof(T);
}

public abstract class SourceGeneratedTestInformation : SourceGeneratedMemberInformation
{
    public required SourceGeneratedParameterInformation[] Parameters { get; init; }
    
    public required SourceGeneratedClassInformation Class { get; init; }
}