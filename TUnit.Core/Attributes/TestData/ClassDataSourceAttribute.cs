using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<
#if NET8_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] 
#endif
    T> : TUnitAttribute where T : new()
{
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] 
#endif
    public Type Type { get; }

    public ClassDataSourceAttribute()
    {
        Type = typeof(T);

        if (!Type.GetConstructors().Any(x => x.IsPublic && x.GetParameters().Length == 0))
        {
            throw new ArgumentException($"{Type.FullName} cannot be used within [ClassData] as it does not have a public constructor.");
        }
    }
    
    public SharedType Shared { get; set; } = SharedType.None;
    public string? Key { get; set; }
}

public enum SharedType
{
    None,
    ForClass,
    Globally,
    Keyed,
}