using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

public record SourceGeneratedMethodInformation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : SourceGeneratedMethodInformation
{
    [field: AllowNull, MaybeNull]
    [field: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public override Type Type => field ??= typeof(T);
}

public abstract record SourceGeneratedMethodInformation : SourceGeneratedMemberInformation
{
    public required SourceGeneratedParameterInformation[] Parameters { get; init; }
    
    public required int GenericTypeCount { get; init; }
    
    public required SourceGeneratedClassInformation Class { get; init; }

    [field: AllowNull, MaybeNull]
    public MethodInfo ReflectionInformation => field ??= GetMethodInfo();

    private MethodInfo GetMethodInfo()
    {
#if NET
        return Type.GetMethod(Name, GenericTypeCount, Parameters.Select(x => x.Type).ToArray())!;
#else
        return Type.GetMethod(Name, Parameters.Select(x => x.Type).ToArray())!;
#endif
    }

    public required Type ReturnType { get; init; }
}