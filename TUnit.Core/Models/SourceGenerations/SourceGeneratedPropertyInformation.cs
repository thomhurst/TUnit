using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public class SourceGeneratedPropertyInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : SourceGeneratedPropertyInformation
{
    public override Type Type { get; } = typeof(T);
}

public abstract class SourceGeneratedPropertyInformation : SourceGeneratedMemberInformation
{
    public required bool IsStatic { get; init; }
}