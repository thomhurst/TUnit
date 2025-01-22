using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public class SourceGeneratedParameterInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : SourceGeneratedParameterInformation
{
    public override Type Type { get; } = typeof(T);
}

public abstract class SourceGeneratedParameterInformation : SourceGeneratedMemberInformation
{
    // public required SourceGeneratedMemberInformation Parent { get; init; }
}