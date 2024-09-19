using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class ClassConstructorAttribute : TUnitAttribute
{
    public abstract Type ClassConstructorType { get; }

    internal ClassConstructorAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public sealed class ClassConstructorAttribute<
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] 
#endif
    T
> : ClassConstructorAttribute where T : IClassConstructor, new()
{
    public override Type ClassConstructorType { get; } = typeof(T);
}