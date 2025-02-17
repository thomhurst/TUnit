using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record SourceGeneratedParameterInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : SourceGeneratedParameterInformation
{
    [field: AllowNull, MaybeNull]
    public override Type Type
    {
        get => field ??= typeof(T);
        init;
    }
}

public abstract record SourceGeneratedParameterInformation : SourceGeneratedMemberInformation;