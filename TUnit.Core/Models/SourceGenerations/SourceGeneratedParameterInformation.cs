using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record SourceGeneratedParameterInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : SourceGeneratedParameterInformation
{
    [field: AllowNull, MaybeNull]
    [field: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public override Type Type => field ??= typeof(T);
}

public abstract record SourceGeneratedParameterInformation : SourceGeneratedMemberInformation;