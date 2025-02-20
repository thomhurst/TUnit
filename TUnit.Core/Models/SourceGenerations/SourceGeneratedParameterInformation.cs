using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record SourceGeneratedParameterInformation<T> : SourceGeneratedParameterInformation
{
    [field: AllowNull, MaybeNull]
    public override Type Type
    {
        get => field ??= typeof(T);
        init;
    }
}

public abstract record SourceGeneratedParameterInformation : SourceGeneratedMemberInformation;