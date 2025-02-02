using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record SourceGeneratedPropertyInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : SourceGeneratedPropertyInformation
{
    [field: AllowNull, MaybeNull]
    [field: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public override Type Type => field ??= typeof(T);
}

public abstract record SourceGeneratedPropertyInformation : SourceGeneratedMemberInformation
{
    public required bool IsStatic { get; init; }
}