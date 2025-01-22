using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public abstract class SourceGeneratedMemberInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : SourceGeneratedMemberInformation
{
    public override Type Type { get; } = typeof(T);
}

public abstract class SourceGeneratedMemberInformation
{
    public abstract Type Type { get; }

    public required string Name { get; init; }

    public required Attribute[] Attributes { get; init; }
}